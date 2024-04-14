using System.Reactive;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattClientPeer
{
    IObservable<Unit> WhenDisconnected { get; }
}