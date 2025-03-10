using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client characteristic with a single property and a specified type for the value </summary>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public interface IGattTypedClientCharacteristic<T, TProp1> : IGattTypedCharacteristic<T>, IGattClientCharacteristic
    where TProp1 : IBleProperty;

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The property </typeparam>
[SuppressMessage(
    "Design",
    "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods"
)]
public class GattTypedClientCharacteristic<T, TProp1>(
    IGattClientCharacteristic characteristic,
    IGattTypedCharacteristic<T>.DecodeFunc onDecode,
    IGattTypedCharacteristic<T>.EncodeFunc onEncode
) : IGattTypedClientCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    /// <summary> The underlying characteristic </summary>
    protected IGattClientCharacteristic Characteristic { get; } = characteristic;
    private readonly IGattTypedCharacteristic<T>.DecodeFunc _onDecode = onDecode;
    private readonly IGattTypedCharacteristic<T>.EncodeFunc _onEncode = onEncode;

    /// <inheritdoc />
    public IGattClientService Service => Characteristic.Service;

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public IReadOnlyCollection<IGattCharacteristicValue> Descriptors => Characteristic.Descriptors;

    IGattCharacteristicDeclaration IGattClientCharacteristic.Declaration => Characteristic.Declaration;
    IGattCharacteristicValue IGattClientCharacteristic.Value => Characteristic.Value;

    /// <inheritdoc />
    public void AddDescriptor(IGattCharacteristicValue value) => Characteristic.AddDescriptor(value);

    void IGattClientCharacteristic.NotifyValue(IGattClientPeer? clientPeer, byte[] value) =>
        Characteristic.NotifyValue(clientPeer, value);

    Task IGattClientCharacteristic.IndicateAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    ) => Characteristic.IndicateAsync(clientPeer, value, cancellationToken);

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Decode" />
    protected T Decode(ReadOnlySpan<byte> source) => _onDecode(source);

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Encode" />
    protected byte[] Encode(T value) => _onEncode(value);

    T IGattTypedCharacteristic<T>.Decode(ReadOnlySpan<byte> source) => Decode(source);

    byte[] IGattTypedCharacteristic<T>.Encode(T value) => Encode(value);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattTypedClientCharacteristic<T, TProp1, TProp2>(
    IGattClientCharacteristic characteristic,
    IGattTypedCharacteristic<T>.DecodeFunc onDecode,
    IGattTypedCharacteristic<T>.EncodeFunc onEncode
)
    : GattTypedClientCharacteristic<T, TProp1>(characteristic, onDecode, onEncode),
        IGattTypedClientCharacteristic<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator GattTypedClientCharacteristic<T, TProp2, TProp1>(
        GattTypedClientCharacteristic<T, TProp1, TProp2> characteristicDeclaration
    )
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new GattTypedClientCharacteristic<T, TProp2, TProp1>(
            characteristicDeclaration.Characteristic,
            characteristicDeclaration.Decode,
            characteristicDeclaration.Encode
        );
    }
}
