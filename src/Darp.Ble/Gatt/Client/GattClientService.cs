using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client service </summary>
/// <param name="uuid"> The UUID of the client service </param>
public abstract class GattClientService(BleUuid uuid) : IGattClientService
{
    private readonly Dictionary<BleUuid, IGattClientCharacteristic> _characteristics = new();

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientCharacteristic> Characteristics => _characteristics;

    /// <inheritdoc />
    public async Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        IGattAttributeValue value,
        GattProperty property,
        CancellationToken cancellationToken)
    {
        IGattClientCharacteristic characteristic = await CreateCharacteristicAsyncCore(uuid, property, cancellationToken).ConfigureAwait(false);
        _characteristics[characteristic.Uuid] = characteristic;
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic to create </param>
    /// <param name="gattProperty"> The property of the characteristic to create </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken);
}