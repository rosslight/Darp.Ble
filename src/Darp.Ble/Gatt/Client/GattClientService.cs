using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client service </summary>
/// <param name="uuid"> The UUID of the client service </param>
/// <param name="type"> The type of the client service </param>
public abstract class GattClientService(BlePeripheral blePeripheral, BleUuid uuid, GattServiceType type)
    : IGattClientService
{
    private readonly List<IGattClientCharacteristic> _characteristics = [];

    /// <summary> The peripheral of the service </summary>
    public IBlePeripheral Peripheral { get; } = blePeripheral;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;

    /// <inheritdoc />
    public IReadOnlyCollection<IGattClientCharacteristic> Characteristics => _characteristics.AsReadOnly();

    /// <inheritdoc />
    public async Task<IGattClientCharacteristic> AddCharacteristicAsync(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    )
    {
        IGattClientCharacteristic characteristic = await CreateCharacteristicAsyncCore(
                uuid,
                gattProperty,
                onRead,
                onWrite,
                cancellationToken
            )
            .ConfigureAwait(false);
        _characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic to create </param>
    /// <param name="gattProperty"> The property of the characteristic to create </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    );
}
