using System.Reactive;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble;

public interface IGattClientService
{
    BleUuid Uuid { get; }
    IReadOnlyDictionary<BleUuid, GattClientCharacteristic> Characteristics { get; }
}

public interface IBlePeripheral
{
    void AddService(IGattClientService service);
    IObservable<IGattClientPeer> WhenConnected { get; }
    IObservable<Unit> WhenDisconnected { get; }
}