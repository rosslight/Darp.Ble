using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public abstract class GattClientService(BleUuid uuid) : IGattClientService
{
    private readonly Dictionary<BleUuid, IGattClientCharacteristic> _characteristics = new();

    public BleUuid Uuid { get; } = uuid;
    public IReadOnlyDictionary<BleUuid, IGattClientCharacteristic> Characteristics => _characteristics;

    public async Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        GattProperty property,
        CancellationToken cancellationToken)
    {
        IGattClientCharacteristic characteristic = await CreateCharacteristicAsyncCore(uuid, property, cancellationToken);
        _characteristics[characteristic.Uuid] = characteristic;
        return characteristic;
    }

    protected abstract Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken);
}