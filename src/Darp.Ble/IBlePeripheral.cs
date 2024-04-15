using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble;

public interface IGattClientService
{
    BleUuid Uuid { get; }
    IReadOnlyDictionary<BleUuid, IGattClientCharacteristic> Characteristics { get; }

    Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid, GattProperty property, CancellationToken cancellationToken);
}

public interface IBlePeripheral
{
    IReadOnlyDictionary<BleUuid, IGattClientService> Services { get; }
    Task<IGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default);
    IObservable<IGattClientPeer> WhenConnected { get; }
    IObservable<IGattClientPeer> WhenDisconnected { get; }
}

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