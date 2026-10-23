using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock.Connections;

/// <summary> Accepts connections from mock centrals in the same air </summary>
internal sealed class MockPeripheralDriver : IPeripheralDriver
{
    private readonly Lock _lock = new();
    private GattServiceDefinition[] _services = [];
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
        sink.OnCentralConnected(link.Peripheral);
        return link;
    }

    public Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken)
    {
        lock (_lock)
            _services = [.. _services, service];
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        if (central is not MockCentralConnection connection)
            throw new ArgumentException("The central is not a mock connection", nameof(central));
        connection.Link.DisconnectFromPeripheral();
        return Task.CompletedTask;
    }
}
