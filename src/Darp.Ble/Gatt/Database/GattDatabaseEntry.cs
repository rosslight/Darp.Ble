using System.Numerics;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Database;

/// <summary> A wrapper entry of the gatt database </summary>
/// <param name="attribute"> The attribute </param>
/// <param name="handle"> The handle of the attribute </param>
public readonly struct GattDatabaseEntry(IGattAttribute attribute, ushort handle)
    : IGattAttribute,
        IEquatable<GattDatabaseEntry>,
        IEqualityOperators<GattDatabaseEntry, GattDatabaseEntry, bool>
{
    private readonly IGattAttribute _attribute = attribute;

    /// <inheritdoc />
    public ushort Handle { get; } = handle;

    /// <inheritdoc />
    public BleUuid AttributeType => _attribute.AttributeType;

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckReadPermissions(clientPeer);

    /// <inheritdoc />
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckWritePermissions(clientPeer);

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        _attribute.WriteValueAsync(clientPeer, value);

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer) => _attribute.ReadValueAsync(clientPeer);

    /// <inheritdoc />
    public bool Equals(GattDatabaseEntry other)
    {
        return _attribute.AttributeType.Equals(other._attribute.AttributeType) && Handle == other.Handle;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GattDatabaseEntry entry && Equals(entry);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_attribute.AttributeType, Handle);

    /// <inheritdoc />
    public static bool operator ==(GattDatabaseEntry left, GattDatabaseEntry right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(GattDatabaseEntry left, GattDatabaseEntry right) => !(left == right);
}
