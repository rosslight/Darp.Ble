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
[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods")]
public class GattTypedClientCharacteristic<T, TProp1>(IGattClientCharacteristic characteristic,
    IGattTypedCharacteristic<T>.ReadValueFunc onRead,
    IGattTypedCharacteristic<T>.WriteValueFunc onWrite)
    : IGattTypedClientCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    /// <summary> The underlying characteristic </summary>
    protected IGattClientCharacteristic Characteristic { get; } = characteristic;
    private readonly IGattTypedCharacteristic<T>.ReadValueFunc _onRead = onRead;
    private readonly IGattTypedCharacteristic<T>.WriteValueFunc _onWrite = onWrite;

    /// <inheritdoc />
    public IGattClientService Service => Characteristic.Service;
    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;
    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors => Characteristic.Descriptors;

    Task<IGattClientDescriptor> IGattClientCharacteristic.AddDescriptorAsync(BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken)
        => Characteristic.AddDescriptorAsync(uuid, onRead, onWrite, cancellationToken);
    ValueTask<byte[]> IGattClientAttribute.GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken)
        => Characteristic.GetValueAsync(clientPeer, cancellationToken);
    ValueTask<GattProtocolStatus> IGattClientAttribute.UpdateValueAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
        => Characteristic.UpdateValueAsync(clientPeer, value, cancellationToken);
    void IGattClientCharacteristic.NotifyValue(IGattClientPeer? clientPeer, byte[] value)
        => Characteristic.NotifyValue(clientPeer, value);
    Task IGattClientCharacteristic.IndicateAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
        => Characteristic.IndicateAsync(clientPeer, value, cancellationToken);

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.ReadValue(System.ReadOnlySpan{byte})" />
    protected T ReadValue(ReadOnlySpan<byte> source) => _onRead(source);
    /// <inheritdoc cref="IGattTypedCharacteristic{T}.WriteValue" />
    protected byte[] WriteValue(T value) => _onWrite(value);

    T IGattTypedCharacteristic<T>.ReadValue(ReadOnlySpan<byte> source) => ReadValue(source);
    byte[] IGattTypedCharacteristic<T>.WriteValue(T value) => WriteValue(value);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattTypedClientCharacteristic<T, TProp1, TProp2>(
    IGattClientCharacteristic characteristic,
    IGattTypedCharacteristic<T>.ReadValueFunc onRead,
    IGattTypedCharacteristic<T>.WriteValueFunc onWrite)
    : GattTypedClientCharacteristic<T, TProp1>(characteristic, onRead, onWrite),
        IGattTypedClientCharacteristic<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator GattTypedClientCharacteristic<T, TProp2, TProp1>(
        GattTypedClientCharacteristic<T, TProp1, TProp2> characteristicDeclaration)
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new GattTypedClientCharacteristic<T, TProp2, TProp1>(characteristicDeclaration.Characteristic,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue);
    }
}
