using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattServerDevice : IAsyncDisposable
{
    IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }
    IObservable<IPlatformSpecificGattServerService> DiscoverServices();
    IObservable<IPlatformSpecificGattServerService> DiscoverService(BleUuid uuid);
}