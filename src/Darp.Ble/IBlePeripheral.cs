using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble;

public interface IGattClientService
{
    BleUuid Uuid { get; }
    IReadOnlyDictionary<BleUuid, GattClientCharacteristic> Characteristics { get; }
}

public interface IBlePeripheral
{
    IReadOnlyDictionary<BleUuid, GattClientService> Services { get; }
    Task<GattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default);
    IObservable<IGattClientPeer> WhenConnected { get; }
    IObservable<IGattClientPeer> WhenDisconnected { get; }
}

public sealed class GattClientService(BleUuid uuid, IPlatformSpecificGattClientService service)
{
    private readonly Dictionary<BleUuid, GattClientCharacteristic> _characteristics = new();
    private readonly IPlatformSpecificGattClientService _service = service;

    public BleUuid Uuid { get; } = uuid;
    public IReadOnlyDictionary<BleUuid, GattClientCharacteristic> Characteristics => _characteristics;

    public async Task<GattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        GattProperty property,
        CancellationToken cancellationToken = default)
    {
        IPlatformSpecificGattClientCharacteristic specificCharacteristic = await _service.AddCharacteristicAsync(uuid, property, cancellationToken);
        var characteristic = new GattClientCharacteristic(uuid, specificCharacteristic);
        _characteristics[characteristic.Uuid] = characteristic;
        return characteristic;
    }
}