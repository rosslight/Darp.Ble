using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

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