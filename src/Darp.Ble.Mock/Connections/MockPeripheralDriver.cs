using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Mock.Connections;

/// <summary> Accepts connections from mock centrals in the same air; owns the links until they go down or the adapter closes </summary>
internal sealed class MockPeripheralDriver(bool includeGattService) : IPeripheralDriver
{
    /// <summary> Whether centrals find the GATT service </summary>
    public bool IncludeGattService { get; } = includeGattService;

    private readonly Lock _lock = new();
    private readonly List<MockLink> _links = [];
    private GattServiceDefinition[] _services = [];
    private bool _closed;
    private (string DeviceName, AppearanceValues Appearance) _identity = ("Darp.Ble", AppearanceValues.Unknown);

    /// <summary> What centrals read from the GAP service </summary>
    public (string DeviceName, AppearanceValues Appearance) Identity
    {
        get
        {
            lock (_lock)
                return _identity;
        }
    }
    private IGattServerSink? _sink;

    /// <summary> The published services, in the order they were added </summary>
    public IReadOnlyList<GattServiceDefinition> Services
    {
        get
        {
            lock (_lock)
                return _services;
        }
    }

    public void Attach(IGattServerSink sink) => _sink = sink;

    /// <summary> A central in the air connects to one of this adapter's advertising sets </summary>
    public MockLink Accept(BleAddress centralAddress, IConnectionSink centralSink)
    {
        IGattServerSink sink = _sink ?? throw new InvalidOperationException("The core did not attach the peripheral");
        var link = new MockLink(centralAddress, centralSink, this, sink);
        lock (_lock)
        {
            if (_closed)
                throw new BleConnectException(BleConnectErrorKind.Unreachable, "The mock peripheral is closed");
            _links.Add(link);
        }
        sink.OnCentralConnected(link.Peripheral);
        return link;
    }

    /// <summary> A link went down </summary>
    public void Forget(MockLink link)
    {
        lock (_lock)
            _links.Remove(link);
    }

    /// <summary> The adapter closes: no central connects any more, and the links still up go down </summary>
    public void Close()
    {
        MockLink[] links;
        lock (_lock)
        {
            _closed = true;
            links = [.. _links];
        }
        foreach (MockLink link in links)
            link.DisconnectFromPeripheral();
    }

    public Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken)
    {
        if (IncludeGattService && service.Uuid == BleUuid.FromUInt16(0x1801))
            throw new InvalidOperationException(
                "The mock serves the GATT service itself; leave it out with includeGattService: false to publish one of your own"
            );
        MockLink[] links;
        lock (_lock)
        {
            _services = [.. _services, service];
            links = [.. _links];
        }
        // Centrals that asked for it learn that the services changed
        foreach (MockLink link in links)
            link.GattService?.OnServicesChanged();
        return Task.CompletedTask;
    }

    public ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        if (central is not MockCentralConnection connection)
            throw new ArgumentException("The central is not a mock connection", nameof(central));
        cancellationToken.ThrowIfCancellationRequested();
        connection.Link.Notify(characteristic, value.Span);
        return ValueTask.CompletedTask;
    }

    public void SetGapIdentity(string deviceName, AppearanceValues appearance)
    {
        lock (_lock)
            _identity = (deviceName, appearance);
    }

    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        if (central is not MockCentralConnection connection)
            throw new ArgumentException("The central is not a mock connection", nameof(central));
        connection.Link.DisconnectFromPeripheral();
        return Task.CompletedTask;
    }
}
