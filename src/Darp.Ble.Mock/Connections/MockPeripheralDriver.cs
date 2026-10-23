using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Mock.Connections;

/// <summary> Accepts connections from mock centrals in the same air; owns the links until they go down or the adapter closes </summary>
internal sealed class MockPeripheralDriver : IPeripheralDriver
{
    private readonly Lock _lock = new();
    private readonly List<MockLink> _links = [];
    private GattServiceDefinition[] _services = [];
    private bool _closed;
    private IGattServerSink? _sink;

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

    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        ((MockCentralConnection)central).Link.DisconnectFromPeripheral();
        return Task.CompletedTask;
    }
}
