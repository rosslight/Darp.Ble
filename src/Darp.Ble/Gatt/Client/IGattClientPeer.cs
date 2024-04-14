using System.Reactive;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientPeer
{
    IObservable<Unit> WhenDisconnected { get; }
}

public sealed class GattClientPeer(IPlatformSpecificGattClientPeer clientPeer) : IGattClientPeer
{
    public IObservable<Unit> WhenDisconnected { get; } = clientPeer.WhenDisconnected;
}