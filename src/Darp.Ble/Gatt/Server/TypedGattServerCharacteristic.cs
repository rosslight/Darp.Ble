using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The interface defining a strongly typed characteristic with a value </summary>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property definition </typeparam>
public interface ITypedGattServerCharacteristic<T, TProp1> : IGattTypedCharacteristic<T>, IGattServerCharacteristic
    where TProp1 : IBleProperty;

/// <summary> The implementation of a strongly typed characteristic </summary>
/// <param name="serverCharacteristic"> The underlying characteristic </param>
/// <param name="onRead"> The callback to read a value from bytes </param>
/// <param name="onWrite"> The callback to write a value to bytes </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property definition </typeparam>
[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods")]
public class TypedGattServerCharacteristic<T, TProp1>(IGattServerCharacteristic serverCharacteristic,
    IGattTypedCharacteristic<T>.ReadValueFunc onRead,
    IGattTypedCharacteristic<T>.WriteValueFunc onWrite)
    : ITypedGattServerCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    private readonly IGattServerCharacteristic _serverCharacteristic = serverCharacteristic;
    private readonly IGattTypedCharacteristic<T>.ReadValueFunc _onRead = onRead;
    private readonly IGattTypedCharacteristic<T>.WriteValueFunc _onWrite = onWrite;

    /// <inheritdoc />
    public IGattServerService Service => _serverCharacteristic.Service;
    /// <inheritdoc />
    public ushort AttributeHandle => _serverCharacteristic.AttributeHandle;
    /// <inheritdoc />
    public BleUuid Uuid => _serverCharacteristic.Uuid;
    /// <inheritdoc />
    public GattProperty Properties => _serverCharacteristic.Properties;

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.ReadValue(System.ReadOnlySpan{byte})" />
    protected T ReadValue(ReadOnlySpan<byte> source) => _onRead(source);
    /// <inheritdoc cref="IGattTypedCharacteristic{T}.WriteValue" />
    protected byte[] WriteValue(T value) => _onWrite(value);

    T IGattTypedCharacteristic<T>.ReadValue(ReadOnlySpan<byte> source) => ReadValue(source);
    byte[] IGattTypedCharacteristic<T>.WriteValue(T value) => WriteValue(value);

    Task IGattServerCharacteristic.WriteAsync(byte[] bytes, CancellationToken cancellationToken) => _serverCharacteristic.WriteAsync(bytes, cancellationToken);
    void IGattServerCharacteristic.WriteWithoutResponse(byte[] bytes) => _serverCharacteristic.WriteWithoutResponse(bytes);
    Task<IAsyncDisposable> IGattServerCharacteristic.OnNotifyAsync<TState>(TState state, Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
        => _serverCharacteristic.OnNotifyAsync(state, onNotify, cancellationToken);
    Task<byte[]> IGattServerCharacteristic.ReadAsync(CancellationToken cancellationToken)
        => _serverCharacteristic.ReadAsync(cancellationToken);
}

/// <summary> The implementation of a strongly typed characteristic </summary>
/// <param name="serverCharacteristic"> The underlying characteristic </param>
/// <param name="onRead"> The callback to read a value from bytes </param>
/// <param name="onWrite"> The callback to write a value to bytes </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property definition </typeparam>
/// <typeparam name="TProp2"> The second property definition </typeparam>
public sealed class TypedGattServerCharacteristic<T, TProp1, TProp2>(IGattServerCharacteristic serverCharacteristic,
    IGattTypedCharacteristic<T>.ReadValueFunc onRead,
    IGattTypedCharacteristic<T>.WriteValueFunc onWrite)
    : TypedGattServerCharacteristic<T, TProp1>(serverCharacteristic, onRead, onWrite),
        ITypedGattServerCharacteristic<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty;