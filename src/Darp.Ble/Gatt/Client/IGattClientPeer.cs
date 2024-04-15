using System.Reactive;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientPeer
{
    bool IsConnected { get; }
    IObservable<Unit> WhenDisconnected { get; }
    BleAddress Address { get; }
}