using System.Buffers.Binary;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;

namespace Darp.Ble.Gatt.Att;

/// <summary> The characteristic declaration attribute </summary>
/// <param name="properties"> The properties of the characteristic </param>
/// <param name="databaseCollection"> The database this characteristic will be added to </param>
/// <param name="value"> The characteristic value </param>
public sealed class GattClientCharacteristicDeclaration(
    GattProperty properties,
    IGattDatabase databaseCollection,
    IGattCharacteristicValue value
) : Client.IGattCharacteristicDeclaration
{
    private readonly IGattDatabase _databaseCollection = databaseCollection;
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
