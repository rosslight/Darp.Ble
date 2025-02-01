using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Gatt.Services;

/// <summary> The service contract for a battery service </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/bas-1-1/"/>
public static class BatteryServiceContract
{
    /// <summary> The uuid of the service </summary>
    /// <value> 0x180F </value>
    public static ServiceDeclaration BatteryService => new (0x180F);
    /// <summary> The battery level in percent </summary>
    /// <value> 0x2A19 </value>
    public static TypedCharacteristicDeclaration<byte, Read, Notify> BatteryLevelCharacteristic { get; } =
        CharacteristicDeclaration.Create<byte, Read, Notify>(0x2A19);

    /// <summary> Add a new DeviceInformationService to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="initialBatteryLevel"> The initial battery level </param>
    /// <param name="batteryLevelDescription"> The description of the battery level characteristic; No descriptor will be added if <c>null</c> </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which contains the service on completion </returns>
    public static async Task<GattClientBatteryService> AddBatteryService(
        this IBlePeripheral peripheral,
        byte initialBatteryLevel = 50,
        string? batteryLevelDescription = "main",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(BatteryService, cancellationToken).ConfigureAwait(false);

        GattTypedClientCharacteristic<byte, Read, Notify> batteryLevelCharacteristic = await service
            .AddCharacteristicAsync(BatteryLevelCharacteristic,
                staticValue: initialBatteryLevel,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (batteryLevelDescription is not null)
        {
            // await batteryLevelCharacteristic.AddUserDescriptionAsync(batteryLevelDescription, cancellationToken);
        }

        return new GattClientBatteryService(service)
        {
            BatteryLevel = batteryLevelCharacteristic,
        };
    }
}

/// <summary> The BatteryService wrapper representing the gatt client </summary>
public sealed class GattClientBatteryService(IGattClientService service) : GattClientServiceProxy(service)
{
    public required GattTypedClientCharacteristic<byte, Read, Notify> BatteryLevel { get; init; }
}

/// <summary> A gatt attribute declaration </summary>
public interface IGattAttributeDeclaration
{
    /// <summary> The uuid of the given declaration </summary>
    BleUuid Uuid { get; }
}

/// <summary> A gatt attribute declaration with a value of a specific type </summary>
/// <typeparam name="T"> The type of the attribute value </typeparam>
public interface IGattAttributeDeclaration<T> : IGattAttributeDeclaration
{
    /// <summary> A delegate specifying how to read a value </summary>
    public delegate T ReadValueFunc(ReadOnlySpan<byte> source);
    /// <summary> A delegate specifying how to write a value </summary>
    public delegate byte[] WriteValueFunc(T value);
    /// <summary> Read the value from a given source of bytes </summary>
    /// <param name="source"> The source to read from </param>
    /// <returns> The value </returns>
    T ReadValue(ReadOnlySpan<byte> source);

    /// <summary> Write a specific value into a destination of bytes </summary>
    /// <param name="value"> The value to write </param>
    /// <returns> The byte array </returns>
    byte[] WriteValue(T value);
}

/// <summary> The gatt service declaration </summary>
public interface IGattServiceDeclaration : IGattAttributeDeclaration
{
    /// <summary> True, if service is a primary service; False, if service is a secondary service </summary>
    GattServiceType Type { get; }
}

public enum GattServiceType
{
    /// <summary> The service type is undefined </summary>
    Undefined,
    /// <summary> The service type is Primary </summary>
    Primary,
    /// <summary> The service type is secondary </summary>
    Secondary,
}

/// <summary> A service declaration </summary>
/// <param name="uuid"> The uuid of the declared service </param>
/// <param name="type"> The type of the declared service. Default is <see cref="GattServiceType.Primary"/> </param>
public sealed class ServiceDeclaration(BleUuid uuid, GattServiceType type = GattServiceType.Primary) : IGattServiceDeclaration
{
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
    /// <inheritdoc />
    public GattServiceType Type { get; } = type;
}

public interface IGattCharacteristicDeclaration
{
    /// <summary> Properties that are part of the characteristic declaration </summary>
    GattProperty Properties { get; }
}

public interface IGattCharacteristicDeclaration<TProp1>
    : IGattCharacteristicDeclaration, IGattAttributeDeclaration
    where TProp1 : IBleProperty;

public class CharacteristicDeclaration<TProp1>(BleUuid uuid) : IGattCharacteristicDeclaration<TProp1>
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public virtual GattProperty Properties => TProp1.GattProperty;
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
}

public sealed class CharacteristicDeclaration<TProp1, TProp2>(BleUuid uuid)
    : CharacteristicDeclaration<TProp1>(uuid), IGattCharacteristicDeclaration<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <inheritdoc />
    public override GattProperty Properties => TProp1.GattProperty | TProp2.GattProperty;

    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    public static implicit operator CharacteristicDeclaration<TProp2, TProp1>(
        CharacteristicDeclaration<TProp1, TProp2> characteristicDeclaration)
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new CharacteristicDeclaration<TProp2, TProp1>(characteristicDeclaration.Uuid);
    }
}

public interface IGattTypedCharacteristicDeclaration<T, TProp1>
    : IGattCharacteristicDeclaration, IGattAttributeDeclaration<T>
    where TProp1 : IBleProperty;

public class TypedCharacteristicDeclaration<T, TProp1>(BleUuid uuid,
    IGattAttributeDeclaration<T>.ReadValueFunc onRead,
    IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
    : IGattTypedCharacteristicDeclaration<T, TProp1>
    where TProp1 : IBleProperty
{
    private readonly IGattAttributeDeclaration<T>.ReadValueFunc _onRead = onRead;
    private readonly IGattAttributeDeclaration<T>.WriteValueFunc _onWrite = onWrite;

    /// <inheritdoc />
    public virtual GattProperty Properties => TProp1.GattProperty;
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc cref="IGattAttributeDeclaration{T}.ReadValue(System.ReadOnlySpan{byte})" />
    protected internal T ReadValue(ReadOnlySpan<byte> source) => _onRead(source);
    /// <inheritdoc cref="IGattAttributeDeclaration{T}.WriteValue" />
    protected internal byte[] WriteValue(T value) => _onWrite(value);

    T IGattAttributeDeclaration<T>.ReadValue(ReadOnlySpan<byte> source) => ReadValue(source);
    byte[] IGattAttributeDeclaration<T>.WriteValue(T value) => WriteValue(value);
}

public sealed class TypedCharacteristicDeclaration<T, TProp1, TProp2>(BleUuid uuid,
    IGattAttributeDeclaration<T>.ReadValueFunc onRead,
    IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
    : TypedCharacteristicDeclaration<T, TProp1>(uuid, onRead, onWrite), IGattTypedCharacteristicDeclaration<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <inheritdoc />
    public override GattProperty Properties => TProp1.GattProperty | TProp2.GattProperty;

    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    public static implicit operator TypedCharacteristicDeclaration<T, TProp2, TProp1>(
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration)
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new TypedCharacteristicDeclaration<T, TProp2, TProp1>(characteristicDeclaration.Uuid,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue);
    }
}

/// <summary> A descriptor declaration </summary>
public interface IGattDescriptorDeclaration : IGattAttributeDeclaration;

/// <summary> The descriptor declaration </summary>
public sealed class DescriptorDeclaration(BleUuid uuid) : IGattDescriptorDeclaration
{
    /// <summary> The Characteristic Extended Properties declaration is a descriptor that defines additional Characteristic Properties </summary>
    public static DescriptorDeclaration CharacteristicExtendedProperties { get; } = new(0x2900);
    /// <summary> The Characteristic User Description declaration is an optional characteristic descriptor that defines a UTF-8 string of variable size that is a user textual description of the Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicUserDescription { get; } = new(0x2901);
    /// <summary> The Client Characteristic Configuration declaration is an optional characteristic descriptor that defines how the characteristic may be configured by a specific client </summary>
    public static DescriptorDeclaration ClientCharacteristicConfiguration { get; } = new(0x2902);
    /// <summary> The Server Characteristic Configuration declaration is an optional characteristic descriptor that defines how the characteristic may be configured for the server </summary>
    public static DescriptorDeclaration ServerCharacteristicConfiguration { get; } = new(0x2903);
    /// <summary> The Characteristic Presentation Format declaration is an optional characteristic descriptor that defines the format of the Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicPresentationFormat { get; } = new(0x2904);
    /// <summary> The Characteristic Aggregate Format declaration is an optional characteristic descriptor that defines the format of an aggregated Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicAggregateFormat { get; } = new(0x2905);

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
}