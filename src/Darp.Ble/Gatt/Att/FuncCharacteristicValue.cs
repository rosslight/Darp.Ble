using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;

namespace Darp.Ble.Gatt.Att;

/// <summary> An attribute value with functional callbacks </summary>
/// <param name="attributeType"> The AttributeType </param>
/// <param name="gattDatabase"> The Database this attribute will be added to </param>
/// <param name="checkReadPermissions"> A callback to check read permissions </param>
/// <param name="onRead"> A callback to read the bytes </param>
/// <param name="checkWritePermissions"> A callback to check write permissions </param>
/// <param name="onWrite"> A callback to write the bytes </param>
public sealed class FuncCharacteristicValue(
    BleUuid attributeType,
    IGattDatabase gattDatabase,
    Func<IGattClientPeer, PermissionCheckStatus> checkReadPermissions,
    IGattAttribute.OnReadAsyncCallback? onRead,
    Func<IGattClientPeer, PermissionCheckStatus> checkWritePermissions,
    IGattAttribute.OnWriteAsyncCallback? onWrite
) : IGattCharacteristicValue
{
    private readonly IGattDatabase _gattDatabase = gattDatabase;
    private readonly Func<IGattClientPeer, PermissionCheckStatus> _checkReadPermissions = checkReadPermissions;
    private readonly IGattAttribute.OnReadAsyncCallback? _onRead = onRead;
    private readonly Func<IGattClientPeer, PermissionCheckStatus> _checkWritePermissions = checkWritePermissions;
    private readonly IGattAttribute.OnWriteAsyncCallback? _onWrite = onWrite;

    /// <summary> An attribute value which is readable </summary>
    /// <param name="attributeType"> The AttributeType </param>
    /// <param name="gattDatabase"> The Database this attribute will be added to </param>
    /// <param name="onRead"> A callback to read the bytes </param>
    public FuncCharacteristicValue(
        BleUuid attributeType,
        IGattDatabase gattDatabase,
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

    /// <summary> An attribute value which is readable </summary>
    /// <param name="attributeType"> The AttributeType </param>
    /// <param name="gattDatabase"> The Database this attribute will be added to </param>
    /// <param name="onWrite"> A callback to write the bytes </param>
    public FuncCharacteristicValue(
        BleUuid attributeType,
        IGattDatabase gattDatabase,
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
