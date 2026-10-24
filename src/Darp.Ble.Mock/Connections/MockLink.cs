using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Mock.Connections;

/// <summary> One connection between two mock adapters. It goes down once; the other side learns it as a remote disconnect </summary>
internal sealed class MockLink
{
    private readonly IConnectionSink _centralSink;
    private readonly IGattServerSink _peripheralSink;
    private readonly Lock _lock = new();
    private bool _down;

    public MockLink(
        BleAddress centralAddress,
        IConnectionSink centralSink,
        MockPeripheralDriver peripheral,
        IGattServerSink peripheralSink
    )
    {
        PeripheralDriver = peripheral;
        _centralSink = centralSink;
        _peripheralSink = peripheralSink;
        Central = new MockConnectionDriver(this);
        Peripheral = new MockCentralConnection(this, centralAddress);
    }

    /// <summary> The peripheral role the central connected to </summary>
    public MockPeripheralDriver PeripheralDriver { get; }

    /// <summary> The central's side </summary>
    public MockConnectionDriver Central { get; }

    /// <summary> The peripheral's side </summary>
    public MockCentralConnection Peripheral { get; }

    public void DisconnectFromCentral()
    {
        if (TakeDown())
            _peripheralSink.OnCentralDisconnected(Peripheral, new DisconnectInfo(DisconnectReason.RemoteRequest));
    }

    public void DisconnectFromPeripheral()
    {
        if (TakeDown())
            _centralSink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));
    }

    private bool TakeDown()
    {
        lock (_lock)
        {
            if (_down)
                return false;
            _down = true;
        }
        PeripheralDriver.Forget(this);
        return true;
    }
}
