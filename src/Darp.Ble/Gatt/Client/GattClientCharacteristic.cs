using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

/// <summary>/// Att error codes to return if the permission check was unsuccessful/// </summary>
public enum PermissionCheckStatus
{
    /// <summary> All permissions are met </summary>
    Success,

    /// <summary> The attribute cannot be read </summary>
    ReadNotPermittedError = 0x02,

    /// <summary> The attribute cannot be written </summary>
    WriteNotPermittedError = 0x03,

    /// <summary> The attribute requires encryption before it can be read or written </summary>
    InsufficientEncryptionError = 0x0F,

    /// <summary> The attribute requires authentication before it can be read or written </summary>
    InsufficientAuthenticationError = 0x05,

    /// <summary> The attribute requires authorization before it can be read or written </summary>
    InsufficientAuthorizationError = 0x08,
}

public sealed class FuncCharacteristicValue(
    BleUuid attributeType,
    GattDatabaseCollection gattDatabase,
    Func<IGattClientPeer, PermissionCheckStatus> checkReadPermissions,
    IGattAttribute.OnReadAsyncCallback? onRead,
    Func<IGattClientPeer, PermissionCheckStatus> checkWritePermissions,
    IGattAttribute.OnWriteAsyncCallback? onWrite
) : IGattCharacteristicValue
{
    private readonly GattDatabaseCollection _gattDatabase = gattDatabase;
    private readonly Func<IGattClientPeer, PermissionCheckStatus> _checkReadPermissions = checkReadPermissions;
    private readonly IGattAttribute.OnReadAsyncCallback? _onRead = onRead;
    private readonly Func<IGattClientPeer, PermissionCheckStatus> _checkWritePermissions = checkWritePermissions;
    private readonly IGattAttribute.OnWriteAsyncCallback? _onWrite = onWrite;

    public FuncCharacteristicValue(
        BleUuid attributeType,
        GattDatabaseCollection gattDatabase,
        IGattAttribute.OnReadAsyncCallback onRead
    )
        : this(
            attributeType,
            gattDatabase,
            checkReadPermissions: _ => PermissionCheckStatus.Success,
            onRead: onRead,
            checkWritePermissions: _ => PermissionCheckStatus.WriteNotPermittedError,
            onWrite: null
        ) { }

    public FuncCharacteristicValue(
        BleUuid attributeType,
        GattDatabaseCollection gattDatabase,
        IGattAttribute.OnWriteAsyncCallback onWrite
    )
        : this(
            attributeType,
            gattDatabase,
            checkReadPermissions: _ => PermissionCheckStatus.ReadNotPermittedError,
            onRead: null,
            checkWritePermissions: _ => PermissionCheckStatus.Success,
            onWrite: onWrite
        ) { }

    /// <inheritdoc />
    public BleUuid AttributeType { get; } = attributeType;

    /// <inheritdoc />
    public ushort Handle => _gattDatabase[this];

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) => _checkReadPermissions(clientPeer);

    /// <inheritdoc />
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        _checkWritePermissions(clientPeer);

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer)
    {
        return _onRead?.Invoke(clientPeer) ?? ValueTask.FromResult<byte[]>([]);
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value)
    {
        return _onWrite?.Invoke(clientPeer, value) ?? ValueTask.FromResult(GattProtocolStatus.WriteRequestRejected);
    }
}

public sealed class GattClientCharacteristicDeclaration(
    GattProperty properties,
    GattDatabaseCollection databaseCollection,
    IGattCharacteristicValue value
) : IGattCharacteristicDeclaration
{
    private readonly GattDatabaseCollection _databaseCollection = databaseCollection;
    private readonly IGattCharacteristicValue _value = value;

    /// <inheritdoc />
    public BleUuid AttributeType => GattDatabaseCollection.CharacteristicType;

    /// <inheritdoc />
    public ushort Handle => _databaseCollection[this];

    /// <inheritdoc />
    public GattProperty Properties { get; } = properties;

    /// <inheritdoc />
    /// <remarks> Read Only, No Authentication, No Authorization </remarks>
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) => PermissionCheckStatus.Success;

    /// <inheritdoc />
    /// <remarks> Read Only, No Authentication, No Authorization </remarks>
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        PermissionCheckStatus.WriteNotPermittedError;

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer)
    {
        var bytes = new byte[3 + (int)_value.AttributeType.Type];
        Span<byte> buffer = bytes;
        buffer[0] = (byte)Properties;
        ushort valueHandle = _databaseCollection[_value];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[1..], valueHandle);
        _value.AttributeType.TryWriteBytes(buffer[3..]);
        return ValueTask.FromResult(bytes);
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        ValueTask.FromResult(GattProtocolStatus.WriteRequestRejected);
}

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="clientService"> The parent client service </param>
/// <param name="uuid"> The UUID of the characteristic </param>
/// <param name="gattProperty"> The property of the characteristic </param>
/// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
/// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
public abstract class GattClientCharacteristic(
    GattClientService clientService,
    GattProperty gattProperty,
    IGattCharacteristicValue value,
    IGattAttribute[] descriptors,
    ILogger<GattClientCharacteristic> logger
) : IGattClientCharacteristic
{
    private readonly List<GattClientDescriptor> _descriptors = [];

    /// <summary> The optional logger </summary>
    protected ILogger<GattClientCharacteristic> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Service.Peripheral.Device.LoggerFactory;

    /// <inheritdoc />
    public IGattClientService Service { get; } = clientService;

    /// <inheritdoc />
    public BleUuid Uuid => Value.AttributeType;

    /// <inheritdoc />
    public GattProperty Properties { get; } = gattProperty;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors =>
        _descriptors.ToDictionary(x => x.Uuid, IGattClientDescriptor (x) => x);

    /// <inheritdoc />
    public IGattCharacteristicDeclaration Declaration { get; } =
        new GattClientCharacteristicDeclaration(gattProperty, clientService.Peripheral.GattDatabase, value);

    /// <inheritdoc />
    public IGattCharacteristicValue Value { get; } = value;

    /// <inheritdoc />
    public IGattClientDescriptor AddDescriptor(IGattCharacteristicValue value)
    {
        if (Descriptors.TryGetValue(value.AttributeType, out IGattClientDescriptor? foundDescriptor))
            return foundDescriptor;
        GattClientDescriptor descriptor = AddDescriptorCore(value);
        _descriptors.Add(descriptor);
        Service.Peripheral.GattDatabase.AddDescriptor(descriptor);
        return descriptor;
    }

    /// <inheritdoc cref="AddDescriptor" />
    protected abstract GattClientDescriptor AddDescriptorCore(IGattCharacteristicValue value);

    /*
    /// <inheritdoc />
    public ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer)
    {
        if (_onRead is null)
            throw new NotSupportedException("Reading is not supported by this characteristic");
        return _onRead(clientPeer);
    }

    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, ReadOnlyMemory<byte> value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, ReadOnlySpan<byte> value)
    {
        if (_onWrite is null)
            throw new NotSupportedException("Writing is not supported by this characteristic");
        return _onWrite(clientPeer, value);
    }
*/
    /// <inheritdoc />
    public void NotifyValue(IGattClientPeer? clientPeer, byte[] value)
    {
        if (clientPeer is not null)
        {
            if (Value.CheckReadPermissions(clientPeer) is PermissionCheckStatus.Success)
            {
                ValueTask<GattProtocolStatus> valueTask = Value.WriteValueAsync(clientPeer, value);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    _ = valueTask.AsTask();
                }
            }
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
        if (clientPeer is not null)
        {
            if (Value.CheckReadPermissions(clientPeer) is PermissionCheckStatus.Success)
            {
                await Value.WriteValueAsync(clientPeer, value).ConfigureAwait(false);
            }
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

    /// <inheritdoc />
    public override string ToString() => $"Characteristic {Uuid}";
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
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors => Characteristic.Descriptors;

    IGattCharacteristicDeclaration IGattClientCharacteristic.Declaration => Characteristic.Declaration;
    IGattCharacteristicValue IGattClientCharacteristic.Value => Characteristic.Value;

    /// <inheritdoc />
    public IGattClientDescriptor AddDescriptor(IGattCharacteristicValue value) => Characteristic.AddDescriptor(value);

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
