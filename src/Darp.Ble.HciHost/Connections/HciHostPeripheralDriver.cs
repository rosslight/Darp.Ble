using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Connections;

/// <summary> The peripheral role: serves the attribute database to every central that connects </summary>
internal sealed partial class HciHostPeripheralDriver : IPeripheralDriver, IDisposable
{
    private readonly HciDevice _device;
    private readonly AttributeDatabase _database;
    private readonly Lock _lock = new();
    private readonly Dictionary<ushort, HciHostCentralLink> _links = [];
    private readonly IDisposable _subscription;
    private IGattServerSink? _sink;
    private bool _transportFailed;

    public HciHostPeripheralDriver(HciDevice device, bool includeGattService)
    {
        _device = device;
        _database = new AttributeDatabase(includeGattService);
        _subscription = device.Host.Subscribe(this);
        device.Host.TransportFailed += OnTransportFailed;
    }

    public void Attach(IGattServerSink sink) => _sink = sink;

    /// <summary> The link of a central by its connection handle, e.g. for the advertising set it connected through </summary>
    public HciHostCentralLink? Find(ushort connectionHandle)
    {
        lock (_lock)
            return _links.GetValueOrDefault(connectionHandle);
    }

    public Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _database.Add(service);
        HciHostCentralLink[] links;
        lock (_lock)
            links = [.. _links.Values.Where(link => link.IsAnnounced)];
        // Centrals that asked for it learn that the database changed
        foreach (HciHostCentralLink link in links)
            link.IndicateServiceChanged();
        return Task.CompletedTask;
    }

    public ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) =>
        central is HciHostCentralLink link
            ? link.NotifyAsync(characteristic, value, cancellationToken)
            : throw new ArgumentException("The central is not an HCI link", nameof(central));

    public void SetGapIdentity(string deviceName, AppearanceValues appearance) =>
        _database.Identity = (deviceName, appearance);

    public async Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        if (central is not HciHostCentralLink link)
            throw new ArgumentException("The central is not an HCI link", nameof(central));
        if (link.Down.IsCompleted)
            return;
        await HciDisconnects.RequestAsync(_device, link.Connection.ConnectionHandle).ConfigureAwait(false);
        // The core learns the end from Disconnection Complete, through OnCentralDisconnected
        await link
            .Down.WaitAsync(_device.Settings.CommandTimeout, _device.TimeProvider, cancellationToken)
            .ConfigureAwait(false);
    }

    [MessageSink]
    private void OnConnectionComplete(HciLeEnhancedConnectionCompleteV1Event hciEvent)
    {
        if (
            hciEvent.Role is not HciLeConnectionRole.Peripheral
            || hciEvent.Status is not HciCommandStatus.Success
            || _sink is not { } sink
            || !_device.TryGetConnection(hciEvent.ConnectionHandle, out AclConnection? connection)
        )
        {
            return;
        }
        var address = new BleAddress(
            (BleAddressType)hciEvent.PeerAddressType,
            (Data.UInt48)hciEvent.PeerAddress.ToUInt64()
        );
        // On the read loop, so the link serves the central's first request
#pragma warning disable CA2000 // The driver owns the link until Disconnection Complete
        var link = new HciHostCentralLink(
            connection,
            address,
            _database,
            sink,
            _device.TimeProvider,
            OnIndicationTimeout
        );
#pragma warning restore CA2000
        lock (_lock)
        {
            // A link that comes up after the transport failed has nobody to serve it
            if (_transportFailed)
            {
                link.Dispose();
                return;
            }
            _links[hciEvent.ConnectionHandle] = link;
        }
        sink.OnCentralConnected(link);
        // The link may have gone down while the core learned of it; that end is reported now, after its start
        DisconnectInfo? ended;
        lock (_lock)
        {
            link.IsAnnounced = true;
            ended = link.PendingEnd;
        }
        if (ended is not null)
            End(link, ended);
    }

    [MessageSink]
    private void OnDisconnectionComplete(HciDisconnectionCompleteEvent hciEvent)
    {
        HciHostCentralLink? link;
        lock (_lock)
        {
#pragma warning disable CA2000 // End disposes the link
            if (!_links.Remove(hciEvent.ConnectionHandle, out link))
                return;
#pragma warning restore CA2000
            if (!link.IsAnnounced)
            {
                link.PendingEnd = HciDisconnects.ToInfo(hciEvent.Reason);
                return;
            }
        }
        End(link, HciDisconnects.ToInfo(hciEvent.Reason));
    }

    private void OnTransportFailed(object? sender, HciTransportFailedEventArgs args)
    {
        var info = new DisconnectInfo(DisconnectReason.TransportLost);
        HciHostCentralLink[] links;
        lock (_lock)
        {
            _transportFailed = true;
            // Links the core does not know yet end once it learned of them
            foreach (HciHostCentralLink pending in _links.Values.Where(link => !link.IsAnnounced))
                pending.PendingEnd = info;
            links = [.. _links.Values.Where(link => link.IsAnnounced)];
            _links.Clear();
        }
        foreach (HciHostCentralLink link in links)
            End(link, info);
    }

    // A central that leaves an indication unconfirmed failed its ATT transaction; its link goes down
    private void OnIndicationTimeout(HciHostCentralLink link) => _ = DisconnectTimedOutAsync(link);

    private async Task DisconnectTimedOutAsync(HciHostCentralLink link)
    {
        try
        {
            await HciDisconnects.RequestAsync(_device, link.Connection.ConnectionHandle).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is BleException or Hci.Exceptions.HciException)
        {
            // The transport failed or the controller refused; the link ends with the transport
        }
    }

    /// <summary> Also for a disconnect this adapter asked for: the core ends the central's connection here </summary>
    private void End(HciHostCentralLink link, DisconnectInfo info)
    {
        if (link.IndicationTimedOut)
            info = new DisconnectInfo(DisconnectReason.AttTimeout, info.NativeCode);
        if (!link.LinkDown(info))
            return;
        link.Dispose();
        _sink?.OnCentralDisconnected(link, info);
    }

    public void Dispose()
    {
        _device.Host.TransportFailed -= OnTransportFailed;
        _subscription.Dispose();
    }
}
