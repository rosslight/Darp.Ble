using System.Numerics;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Database;

/// <summary> A wrapper entry of the gatt database </summary>
/// <param name="attribute"> The attribute </param>
/// <param name="handle"> The handle of the attribute </param>
public readonly struct GattDatabaseEntry(IGattDatabase gattDatabase, IGattAttribute attribute, ushort handle)
    : IGattAttribute,
        IEquatable<GattDatabaseEntry>,
        IEqualityOperators<GattDatabaseEntry, GattDatabaseEntry, bool>
{
    private readonly IGattDatabase _gattDatabase = gattDatabase;
    private readonly IGattAttribute _attribute = attribute;

    /// <inheritdoc />
    public ushort Handle { get; } = handle;

    /// <inheritdoc />
    public BleUuid AttributeType => _attribute.AttributeType;

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckReadPermissions(clientPeer);

    /// <summary> True, if the attribute is a group type; False, otherwise </summary>
    public bool IsGroupType => GattDatabaseCollection.IsGroupType(_attribute.AttributeType);

    /// <summary> Tries to get the end group handle </summary>
    /// <param name="endHandle"> The end handle if a group. The start handle otherwise </param>
    /// <returns> True, if the attribute is a group type; False, otherwise </returns>
    public bool TryGetGroupEndHandle(out ushort endHandle)
    {
        if (!IsGroupType)
        {
            endHandle = Handle;
            return false;
        }
        endHandle = _gattDatabase.GetGroupEndHandle(Handle);
        return true;
    }

    /// <inheritdoc />
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckWritePermissions(clientPeer);

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        IServiceProvider serviceProvider
    ) => _attribute.WriteValueAsync(clientPeer, value, serviceProvider);

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer, IServiceProvider serviceProvider) =>
        _attribute.ReadValueAsync(clientPeer, serviceProvider);

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
