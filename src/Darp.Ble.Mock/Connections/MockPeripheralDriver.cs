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

    // Set once by the core before the adapter is handed out
    private IGattServerSink? _sink;

    // Guards the fields below: centrals in the air connect while the app adds services
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

    /// <summary> The added services, in order </summary>
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
    public MockLink Accept(MockCentralDriver central, IConnectionSink centralSink)
    {
        IGattServerSink sink = _sink ?? throw new InvalidOperationException("The core did not attach the peripheral");
        var link = new MockLink(central, centralSink, this, sink);
        lock (_lock)
        {
            if (_closed)
                throw new BleConnectException(BleConnectErrorKind.Unreachable, "The mock peripheral is closed");
        }
        sink.OnCentralConnected(link.Peripheral);
        // Known only once the core admitted the central, so nothing can end the link before the core knows it
        bool closed;
        lock (_lock)
        {
            closed = _closed;
            if (!closed)
                _links.Add(link);
        }
        // Closing did not see the link yet; it goes down as closing would have taken it
        if (closed)
            link.DisconnectFromPeripheral();
        // A link that went down meanwhile already forgot itself, before it was added
        else if (link.IsDown)
            Forget(link);
        return link;
    }

    /// <summary> The links that are up </summary>
    public MockLink[] Links
    {
        get
        {
            lock (_lock)
                return [.. _links];
        }
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
                "The mock serves the GATT service itself; leave it out with includeGattService: false to add one of your own"
            );
        lock (_lock)
            _services = [.. _services, service];
        return Task.CompletedTask;
    }

    public ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ((MockCentralConnection)central).Link.Notify(characteristic, value.Span);
        return ValueTask.CompletedTask;
    }

    public void SetGapIdentity(string deviceName, AppearanceValues appearance)
    {
        lock (_lock)
            _identity = (deviceName, appearance);
    }

    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        ((MockCentralConnection)central).Link.DisconnectFromPeripheral();
        return Task.CompletedTask;
    }
}
