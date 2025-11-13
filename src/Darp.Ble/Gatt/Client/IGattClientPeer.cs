using System.Reactive;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> Defined the peer of a gatt client </summary>
public interface IGattClientPeer
{
    /// <summary> The peripheral that controls the connection </summary>
    IBlePeripheral Peripheral { get; }

    /// <summary> The BLE address of the peer central device </summary>
    BleAddress Address { get; }

    /// <summary> True, if the peer is connected </summary>
    bool IsConnected { get; }

    /// <summary> An observable to notify about disconnection </summary>
    IObservable<Unit> WhenDisconnected { get; }
}
