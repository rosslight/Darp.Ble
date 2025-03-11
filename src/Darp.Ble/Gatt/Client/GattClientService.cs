using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
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
    private readonly AttributeCollection<IGattClientCharacteristic> _characteristics = new(characteristic =>
        characteristic.Uuid
    );

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
    public IGattServiceDeclaration Declaration { get; } =
        new GattServiceDeclaration(blePeripheral.GattDatabase, uuid, type);

    /// <inheritdoc />
    public IReadonlyAttributeCollection<IGattClientCharacteristic> Characteristics => _characteristics;

    /// <inheritdoc />
    public IGattClientCharacteristic AddCharacteristic(
        GattProperty properties,
        IGattCharacteristicValue value,
        IGattCharacteristicValue[] descriptors
    )
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        GattClientCharacteristic characteristic = CreateCharacteristicCore(properties, value);
        _characteristics.Add(characteristic);
        Peripheral.GattDatabase.AddCharacteristic(characteristic);

        foreach (IGattCharacteristicValue descriptor in descriptors)
        {
            characteristic.AddDescriptor(descriptor);
        }
        if (
            properties.HasFlag(GattProperty.Notify)
            && !characteristic.Descriptors.ContainsAny(DescriptorDeclaration.ClientCharacteristicConfiguration.Uuid)
        )
        {
            characteristic.AddClientCharacteristicConfiguration();
        }
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="properties"> The properties of the characteristic to create </param>
    /// <param name="value"> The characteristic value </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract GattClientCharacteristic CreateCharacteristicCore(
        GattProperty properties,
        IGattCharacteristicValue value
    );
}
