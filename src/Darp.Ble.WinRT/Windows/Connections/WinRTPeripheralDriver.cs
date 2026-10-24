using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.WinRT.Gatt;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// Hosting on Windows. Windows owns the GAP, GATT, Device Information and Scan Parameters services, so publishing
/// those fails. Windows serves a service only while it advertises it, so added services are live while a connectable
/// advertising set runs. A central becomes known through its first request or subscription; Windows has no event for a
/// connection alone, and this app cannot take a central's link down, only end its own part (D11)
/// </summary>
internal sealed class WinRTPeripheralDriver(ILogger logger) : IPeripheralDriver, IDisposable
{
    private static readonly HashSet<BleUuid> Reserved =
    [
        BleUuid.FromUInt16(0x1800),
        BleUuid.FromUInt16(0x1801),
        BleUuid.FromUInt16(0x180A),
        BleUuid.FromUInt16(0x1813),
    ];

    private readonly Lock _lock = new();

    // Resolving and announcing a central, and ending it, happen under this lock: no callback sees a central the core
    // does not know yet or no longer knows
    private readonly Lock _announcing = new();

    // Services are added and advertised one change at a time, through their whole native calls
    private readonly SemaphoreSlim _publishing = new(1, 1);

    // Subscription lists are applied one at a time, so an older list cannot win over a newer one
    private readonly SemaphoreSlim _subscribing = new(1, 1);
    private readonly List<WinRTHostedService> _services = [];
    private readonly CentralSessions<GattSession, WinRTCentralLink> _sessions = new();
    private readonly SubscriptionSnapshots<GattCharacteristicDefinition, WinRTCentralLink> _subscriptions = new();
    private readonly Dictionary<string, BleAddress> _addresses = new(StringComparer.Ordinal);
    private IGattServerSink? _sink;
    private int _connectableSets;

    public ILogger Logger { get; } = logger;

    public IGattServerSink Sink =>
        _sink ?? throw new InvalidOperationException("The core did not attach the peripheral");

    public void Attach(IGattServerSink sink) => _sink = sink;

    public async Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken)
    {
        if (Reserved.Contains(service.Uuid))
            throw new InvalidOperationException($"Windows serves the service {service.Uuid} itself");
        if (!service.IsPrimary)
            throw new NotSupportedException("Windows publishes primary services only");
        await _publishing.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            WinRTHostedService? hosted = await WinRTHostedService
                .CreateAsync(this, service, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                if (_connectableSets > 0)
                    await hosted.StartAdvertisingAsync(cancellationToken).ConfigureAwait(false);
                lock (_lock)
                    _services.Add(hosted);
                hosted = null;
            }
            finally
            {
                // Not added: nothing of it stays
                hosted?.Dispose();
            }
        }
        finally
        {
            _publishing.Release();
        }
    }

    /// <summary> Whether any service is published; connectable advertising needs one </summary>
    public bool HasServices
    {
        get
        {
            lock (_lock)
                return _services.Count > 0;
        }
    }

    /// <summary>
    /// A connectable advertising set started or stopped: the services are advertised while one runs. The count changes
    /// only when starting succeeded; a failure stops the services it started
    /// </summary>
    public async Task SetAdvertisedAsync(bool advertised, CancellationToken cancellationToken)
    {
        await _publishing.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            int sets = Math.Max(0, _connectableSets + (advertised ? 1 : -1));
            WinRTHostedService[] services;
            lock (_lock)
                services = [.. _services];
            if (sets > 0 && _connectableSets == 0)
                await StartAdvertisingAsync(services, cancellationToken).ConfigureAwait(false);
            else if (sets == 0)
            {
                foreach (WinRTHostedService service in services)
                    service.StopAdvertising();
            }
            _connectableSets = sets;
        }
        finally
        {
            _publishing.Release();
        }
    }

    private static async Task StartAdvertisingAsync(WinRTHostedService[] services, CancellationToken cancellationToken)
    {
        List<WinRTHostedService> started = [];
        try
        {
            foreach (WinRTHostedService service in services)
            {
                await service.StartAdvertisingAsync(cancellationToken).ConfigureAwait(false);
                started.Add(service);
            }
        }
        catch
        {
            foreach (WinRTHostedService service in started)
                service.StopAdvertising();
            throw;
        }
    }

    public async ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        if (central is not WinRTCentralLink link)
            throw new ArgumentException("The central is not a Windows connection", nameof(central));
        cancellationToken.ThrowIfCancellationRequested();
        GattLocalCharacteristic? native;
        lock (_lock)
            native = _services
                .Select(service => service.Find(characteristic))
                .FirstOrDefault(found => found is not null);
        if (native is null || !_subscriptions.IsSubscribed(characteristic, link))
            return;
        // The client of this central right now; a session that closed is no longer listed
        GattSubscribedClient? client = native.SubscribedClients.FirstOrDefault(c =>
            string.Equals(c.Session.DeviceId.Id, link.DeviceId, StringComparison.Ordinal)
        );
        if (client is null)
            return;
        // Longer values are cut to what the client takes, as ATT does
        int length = Math.Min(value.Length, client.MaxNotificationSize);
        GattClientNotificationResult result = await native
            .NotifyValueAsync(value[..length].ToArray().AsBuffer(), client)
            .AsTask()
            .ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new BleException($"Windows could not notify the central: {result.Status}");
    }

    public void SetGapIdentity(string deviceName, AppearanceValues appearance) =>
        throw new NotSupportedException("Windows serves the GAP service itself");

    /// <summary> Ends this app's connection to the central; Windows may keep the link for other apps </summary>
    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        lock (_announcing)
        {
            if (central is WinRTCentralLink link && _sessions.End(link))
                End(link, new DisconnectInfo(DisconnectReason.LocalRequest));
        }
        return Task.CompletedTask;
    }

    /// <summary> The central of a session; announced to the core before its first request. Null for a session that closed </summary>
    public async Task<WinRTCentralLink?> ResolveAsync(GattSession session)
    {
        string deviceId = session.DeviceId.Id;
        BleAddress address = await AddressOfAsync(deviceId).ConfigureAwait(false);
        lock (_announcing)
        {
            (WinRTCentralLink? link, bool isNewCentral, bool isNewSession) = _sessions.Resolve(
                deviceId,
                session,
                () => new WinRTCentralLink(deviceId, address)
            );
            if (link is null)
                return null;
            if (isNewCentral)
                Sink.OnCentralConnected(link);
            if (isNewSession)
            {
                session.SessionStatusChanged += OnSessionStatusChanged;
                // It may have closed before the handler was attached
                if (session.SessionStatus is GattSessionStatus.Closed)
                {
                    Close(session);
                    return null;
                }
            }
            return link;
        }
    }

    /// <summary> Windows reports a new list of subscribed clients; the core learns the changes against the list now </summary>
    public async Task OnSubscribedClientsChangedAsync(
        GattCharacteristicDefinition characteristic,
        GattLocalCharacteristic native
    )
    {
        try
        {
            await _subscribing.WaitAsync().ConfigureAwait(false);
            try
            {
                List<WinRTCentralLink> links = [];
                foreach (GattSubscribedClient client in native.SubscribedClients)
                {
                    if (await ResolveAsync(client.Session).ConfigureAwait(false) is { } link)
                        links.Add(link);
                }
                (IReadOnlyList<WinRTCentralLink> added, IReadOnlyList<WinRTCentralLink> removed) =
                    _subscriptions.Update(characteristic, links);
                foreach (WinRTCentralLink link in added)
                    Sink.OnSubscriptionChanged(link, characteristic, enabled: true);
                foreach (WinRTCentralLink link in removed)
                    Sink.OnSubscriptionChanged(link, characteristic, enabled: false);
            }
            finally
            {
                _subscribing.Release();
            }
        }
        catch (Exception e)
        {
            // An event handler must not throw into Windows
            Logger.LogHostedRequestFailed(e);
        }
    }

    private void OnSessionStatusChanged(GattSession sender, GattSessionStatusChangedEventArgs args)
    {
        if (args.Status is GattSessionStatus.Closed)
            Close(sender);
    }

    /// <summary> A session closed; ends its central when it was the last </summary>
    private void Close(GattSession session)
    {
        session.SessionStatusChanged -= OnSessionStatusChanged;
        lock (_announcing)
        {
            if (_sessions.Close(session) is { } link)
                End(link, new DisconnectInfo(DisconnectReason.RemoteRequest));
        }
    }

    private void End(WinRTCentralLink link, DisconnectInfo info)
    {
        _subscriptions.Forget(link);
        Sink.OnCentralDisconnected(link, info);
    }

    private async Task<BleAddress> AddressOfAsync(string deviceId)
    {
        lock (_lock)
        {
            if (_addresses.TryGetValue(deviceId, out BleAddress? known))
                return known;
        }
        using BluetoothLEDevice? device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask().ConfigureAwait(false);
        BleAddress address = device is null
            ? BleAddress.NotAvailable
            : WinRTMapping.ToAddress(device.BluetoothAddress, device.BluetoothAddressType);
        lock (_lock)
            _addresses[deviceId] = address;
        return address;
    }

    public void Dispose()
    {
        WinRTHostedService[] services;
        lock (_lock)
        {
            services = [.. _services];
            _services.Clear();
        }
        foreach (WinRTHostedService service in services)
            service.Dispose();
        _publishing.Dispose();
        _subscribing.Dispose();
    }
}
