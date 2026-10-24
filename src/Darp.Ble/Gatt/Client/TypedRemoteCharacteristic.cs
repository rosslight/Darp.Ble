using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A discovered characteristic that supports <typeparamref name="TProp1"/> and whose value is a <typeparamref name="T"/> </summary>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProp1"> The property marker </typeparam>
public sealed class TypedRemoteCharacteristic<T, TProp1> : ITypedRemoteCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    internal TypedRemoteCharacteristic(RemoteGattCharacteristic characteristic, IGattTypedCharacteristic<T> declaration)
    {
        Characteristic = characteristic;
        Declaration = declaration;
    }

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public RemoteGattCharacteristic Characteristic { get; }

    /// <inheritdoc />
    public IGattTypedCharacteristic<T> Declaration { get; }
}

/// <summary> A discovered characteristic that supports <typeparamref name="TProp1"/> and <typeparamref name="TProp2"/> and whose value is a <typeparamref name="T"/> </summary>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProp1"> The first property marker </typeparam>
/// <typeparam name="TProp2"> The second property marker </typeparam>
public sealed class TypedRemoteCharacteristic<T, TProp1, TProp2> : ITypedRemoteCharacteristic<T, TProp1, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    internal TypedRemoteCharacteristic(RemoteGattCharacteristic characteristic, IGattTypedCharacteristic<T> declaration)
    {
        Characteristic = characteristic;
        Declaration = declaration;
    }

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public RemoteGattCharacteristic Characteristic { get; }

    /// <inheritdoc />
    public IGattTypedCharacteristic<T> Declaration { get; }
}
