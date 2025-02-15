using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client service </summary>
/// <param name="uuid"> The UUID of the client service </param>
/// <param name="type"> The type of the client service </param>
public abstract class GattClientService(
    BlePeripheral blePeripheral,
    BleUuid uuid,
    GattServiceType type,
    ILogger<GattClientService> logger
) : IGattClientService
{
    private readonly List<GattClientCharacteristic> _characteristics = [];

    /// <summary> The optional logger </summary>
    protected ILogger<GattClientService> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Peripheral.Device.LoggerFactory;

    /// <summary> The peripheral of the service </summary>
    public IBlePeripheral Peripheral { get; } = blePeripheral;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;

    /// <inheritdoc />
    public virtual ushort Handle => Peripheral.GattDatabase[this];

    /// <inheritdoc />
    public BleUuid AttributeType =>
        Type is GattServiceType.Secondary
            ? GattDatabaseCollection.SecondaryServiceType
            : GattDatabaseCollection.PrimaryServiceType;

    /// <inheritdoc />
    /// <remarks> Either is 0x1800 for primary services or 0x1801 for secondary services </remarks>
    public byte[] AttributeValue { get; } = uuid.ToByteArray();

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
        GattClientCharacteristic characteristic = await CreateCharacteristicAsyncCore(
                uuid,
                gattProperty,
                onRead,
                onWrite,
                cancellationToken
            )
            .ConfigureAwait(false);
        _characteristics.Add(characteristic);
        Peripheral.GattDatabase.AddCharacteristic(characteristic);
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic to create </param>
    /// <param name="gattProperty"> The property of the characteristic to create </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract Task<GattClientCharacteristic> CreateCharacteristicAsyncCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    );
}
