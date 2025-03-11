using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;

namespace Darp.Ble.Gatt.Att;

internal sealed class GattServiceDeclaration(IGattDatabase gattDatabase, BleUuid uuid, GattServiceType type)
    : IGattServiceDeclaration
{
    private readonly IGattDatabase _gattDatabase = gattDatabase;
    private readonly BleUuid _uuid = uuid;

    public BleUuid AttributeType { get; } =
        type is GattServiceType.Secondary
            ? GattDatabaseCollection.SecondaryServiceType
            : GattDatabaseCollection.PrimaryServiceType;

    public ushort Handle => _gattDatabase[this];

    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) => PermissionCheckStatus.Success;

    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        PermissionCheckStatus.WriteNotPermittedError;

    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer) => ValueTask.FromResult(_uuid.ToByteArray());

    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        ValueTask.FromResult(GattProtocolStatus.WriteRequestRejected);
}
