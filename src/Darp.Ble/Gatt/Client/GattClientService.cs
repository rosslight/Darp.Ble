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
    public IGattAttribute Declaration { get; } =
        new FuncCharacteristicValue(
            type is GattServiceType.Secondary
                ? GattDatabaseCollection.SecondaryServiceType
                : GattDatabaseCollection.PrimaryServiceType,
            blePeripheral.GattDatabase,
            _ => ValueTask.FromResult(uuid.ToByteArray())
        );

    /// <inheritdoc />
    public IReadOnlyCollection<IGattClientCharacteristic> Characteristics => _characteristics.AsReadOnly();

    /// <inheritdoc />
    public IGattClientCharacteristic AddCharacteristic(
        GattProperty properties,
        IGattCharacteristicValue value,
        IGattAttribute[] descriptors
    )
    {
        GattClientCharacteristic characteristic = CreateCharacteristicCore(properties, value, descriptors);
        _characteristics.Add(characteristic);
        Peripheral.GattDatabase.AddCharacteristic(characteristic);
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic to create </param>
    /// <param name="gattProperty"> The property of the characteristic to create </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract GattClientCharacteristic CreateCharacteristicCore(
        GattProperty properties,
        IGattCharacteristicValue value,
        IGattAttribute[] descriptors
    );
}
