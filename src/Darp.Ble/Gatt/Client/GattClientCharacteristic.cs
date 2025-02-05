using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="clientService"> The parent client service </param>
/// <param name="uuid"> The UUID of the characteristic </param>
/// <param name="gattProperty"> The property of the characteristic </param>
/// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
/// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
public abstract class GattClientCharacteristic(
    GattClientService clientService,
    BleUuid uuid,
    GattProperty gattProperty,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite,
    GattClientCharacteristic? previousCharacteristic,
    ILogger<GattClientCharacteristic> logger
) : IGattClientCharacteristic, IGattAttribute
{
    private readonly List<GattClientDescriptor> _descriptors = [];
    private readonly IGattClientAttribute.OnReadCallback? _onRead = onRead;
    private readonly IGattClientAttribute.OnWriteCallback? _onWrite = onWrite;
    private readonly GattClientCharacteristic? _previousCharacteristic = previousCharacteristic;

    /// <summary> The optional logger </summary>
    protected ILogger<GattClientCharacteristic> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Service.Peripheral.Device.LoggerFactory;

    /// <inheritdoc />
    public IGattClientService Service { get; } = clientService;

    public virtual ushort StartHandle => _previousCharacteristic?.EndHandle ?? 0;

    public virtual ushort EndHandle
    {
        get
        {
            ushort handleOffset = StartHandle;
            if (_descriptors.Count is 0)
                return (ushort)(handleOffset + 1);
            return (ushort)(_descriptors[^1].EndHandle + 1);
        }
    }

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattProperty Properties { get; } = gattProperty;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors =>
        _descriptors.ToDictionary(x => x.Uuid, IGattClientDescriptor (x) => x);

    /// <inheritdoc />
    public async Task<IGattClientDescriptor> AddDescriptorAsync(
        BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead = null,
        IGattClientAttribute.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default
    )
    {
        if (Descriptors.TryGetValue(uuid, out IGattClientDescriptor? foundDescriptor))
            return foundDescriptor;
        GattClientDescriptor? previousDescriptor = _descriptors.Count > 0 ? _descriptors[^1] : null;
        GattClientDescriptor descriptor = await AddDescriptorAsyncCore(
                uuid,
                onRead,
                onWrite,
                previousDescriptor,
                cancellationToken
            )
            .ConfigureAwait(false);
        _descriptors.Add(descriptor);
        return descriptor;
    }

    /// <inheritdoc cref="AddDescriptorAsync" />
    protected abstract Task<GattClientDescriptor> AddDescriptorAsyncCore(
        BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        GattClientDescriptor? previousDescriptor,
        CancellationToken cancellationToken
    );

    /// <inheritdoc />
    public ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken)
    {
        if (_onRead is null)
            throw new NotSupportedException("Reading is not supported by this characteristic");
        return _onRead(clientPeer, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> UpdateValueAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    )
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
                writeTask.AsTask().GetAwaiter().GetResult();
            }
        }
        if (clientPeer is not null)
        {
            NotifyCore(clientPeer, value);
        }
        else
        {
            foreach (IGattClientPeer connectedPeer in Service.Peripheral.PeerDevices.Values)
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
            IEnumerable<Task> tasks = Service.Peripheral.PeerDevices.Values.Select(connectedPeer =>
                IndicateAsyncCore(connectedPeer, value, cancellationToken)
            );
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
    protected abstract Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    );
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
[SuppressMessage(
    "Design",
    "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods"
)]
public class GattClientCharacteristic<TProp1>(IGattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    /// <summary> The underlying characteristic </summary>
    protected IGattClientCharacteristic Characteristic { get; } = characteristic;

    /// <inheritdoc />
    public IGattClientService Service => Characteristic.Service;

    /// <inheritdoc />
    public ushort StartHandle => Characteristic.StartHandle;

    /// <inheritdoc />
    public ushort EndHandle => Characteristic.EndHandle;

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors => Characteristic.Descriptors;

    /// <inheritdoc />
    public Task<IGattClientDescriptor> AddDescriptorAsync(
        BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead = null,
        IGattClientAttribute.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default
    ) => Characteristic.AddDescriptorAsync(uuid, onRead, onWrite, cancellationToken);

    ValueTask<byte[]> IGattClientAttribute.GetValueAsync(
        IGattClientPeer? clientPeer,
        CancellationToken cancellationToken
    ) => Characteristic.GetValueAsync(clientPeer, cancellationToken);

    ValueTask<GattProtocolStatus> IGattClientAttribute.UpdateValueAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    ) => Characteristic.UpdateValueAsync(clientPeer, value, cancellationToken);

    void IGattClientCharacteristic.NotifyValue(IGattClientPeer? clientPeer, byte[] value) =>
        Characteristic.NotifyValue(clientPeer, value);

    Task IGattClientCharacteristic.IndicateAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    ) => Characteristic.IndicateAsync(clientPeer, value, cancellationToken);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattClientCharacteristic<TProp1, TProp2>(IGattClientCharacteristic characteristic)
    : GattClientCharacteristic<TProp1>(characteristic),
        IGattClientCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator GattClientCharacteristic<TProp2, TProp1>(
        GattClientCharacteristic<TProp1, TProp2> characteristicDeclaration
    )
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new GattClientCharacteristic<TProp2, TProp1>(characteristicDeclaration.Characteristic);
    }
}
