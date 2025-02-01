using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="uuid"> The UUID of the characteristic </param>
/// <param name="gattProperty"> The property of the characteristic </param>
public abstract class GattClientCharacteristic(GattClientService clientService,
    BleUuid uuid,
    GattProperty gattProperty,
    IGattClientService.OnReadCallback? onRead,
    IGattClientService.OnWriteCallback? onWrite) : IGattClientCharacteristic
{
    private readonly GattClientService _clientService = clientService;
    private readonly IGattClientService.OnReadCallback? _onRead = onRead;
    private readonly IGattClientService.OnWriteCallback? _onWrite = onWrite;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
    /// <inheritdoc />
    public GattProperty Property { get; } = gattProperty;

    /// <inheritdoc />
    public ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken)
    {
        if (_onRead is null)
            throw new NotSupportedException("Reading is not supported by this characteristic");
        return _onRead(clientPeer, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
    {
        if (_onWrite is null)
            throw new NotSupportedException("Writing is not supported by this characteristic");
        return _onWrite(clientPeer, value, cancellationToken);
    }

    /// <inheritdoc />
    public void NotifyValue(IGattClientPeer? clientPeer, byte[] value)
    {
        if (_onWrite is not null)
        {
            ValueTask<GattProtocolStatus> writeTask = _onWrite(clientPeer, value, CancellationToken.None);
            if (!writeTask.IsCompleted)
            {
                writeTask.AsTask().RunSynchronously();
            }
        }
        if (clientPeer is not null)
        {
            NotifyCore(clientPeer, value);
        }
        else
        {
            foreach (IGattClientPeer connectedPeer in _clientService.Peripheral.PeerDevices.Values)
            {
                NotifyCore(connectedPeer, value);
            }
        }
    }

    /// <inheritdoc />
    public async Task IndicateAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
    {
        if (_onWrite is not null)
        {
            await _onWrite(clientPeer, value, cancellationToken).ConfigureAwait(false);
        }
        if (clientPeer is not null)
        {
            await IndicateAsyncCore(clientPeer, value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            IEnumerable<Task> tasks = _clientService.Peripheral.PeerDevices.Values
                .Select(connectedPeer => IndicateAsyncCore(connectedPeer, value, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary> Notify a connected clientPeer of a new value </summary>
    /// <param name="clientPeer"> The client to notify </param>
    /// <param name="value"> The value to be used </param>
    protected abstract void NotifyCore(IGattClientPeer clientPeer, byte[] value);

    /// <summary> Notify a connected clientPeer of a new value </summary>
    /// <param name="clientPeer"> The client to notify </param>
    /// <param name="value"> The value to be used </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A task with the result of the indication </returns>
    protected abstract Task IndicateAsyncCore(IGattClientPeer clientPeer, byte[] value, CancellationToken cancellationToken);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
public class GattClientCharacteristic<TProp1>(IGattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public GattProperty Property => Characteristic.Property;
    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = characteristic;
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattClientCharacteristic<TProp1, TProp2>(IGattClientCharacteristic characteristic)
    : GattClientCharacteristic<TProp1>(characteristic), IGattClientCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty;

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The property </typeparam>
public class GattTypedClientCharacteristic<T, TProp1>(IGattClientCharacteristic characteristic,
    IGattAttributeDeclaration<T>.ReadValueFunc onRead,
    IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
    : IGattTypedClientCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    private readonly IGattAttributeDeclaration<T>.ReadValueFunc _onRead = onRead;
    private readonly IGattAttributeDeclaration<T>.WriteValueFunc _onWrite = onWrite;

    /// <inheritdoc />
    public GattProperty Property => Characteristic.Property;
    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = characteristic;

    /// <inheritdoc cref="IGattAttributeDeclaration{T}.ReadValue(System.ReadOnlySpan{byte})" />
    protected internal T ReadValue(ReadOnlySpan<byte> source) => _onRead(source);
    /// <inheritdoc cref="IGattAttributeDeclaration{T}.WriteValue" />
    protected internal byte[] WriteValue(T value) => _onWrite(value);

    T IGattTypedClientCharacteristic<T, TProp1>.ReadValue(ReadOnlySpan<byte> source) => ReadValue(source);
    byte[] IGattTypedClientCharacteristic<T, TProp1>.WriteValue(T value) => WriteValue(value);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattTypedClientCharacteristic<T, TProp1, TProp2>(
    IGattClientCharacteristic characteristic,
    IGattAttributeDeclaration<T>.ReadValueFunc onRead,
    IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
    : GattTypedClientCharacteristic<T, TProp1>(characteristic, onRead, onWrite),
        IGattTypedClientCharacteristic<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    T IGattTypedClientCharacteristic<T, TProp2>.ReadValue(ReadOnlySpan<byte> source) => ReadValue(source);
    byte[] IGattTypedClientCharacteristic<T, TProp2>.WriteValue(T value) => WriteValue(value);
}