using System.Numerics;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Database;

/// <summary> A wrapper entry of the gatt database </summary>
/// <param name="attribute"> The attribute </param>
/// <param name="handle"> The handle of the first attribute of the group </param>
/// <param name="endGroupHandle"> The last handle of the group </param>
public readonly struct GattDatabaseGroupEntry(IGattAttribute attribute, ushort handle, ushort endGroupHandle)
    : IGattAttribute,
        IEquatable<GattDatabaseGroupEntry>,
        IEqualityOperators<GattDatabaseGroupEntry, GattDatabaseGroupEntry, bool>
{
    private readonly IGattAttribute _attribute = attribute;

    /// <summary> The first handle of the attribute group </summary>
    public ushort Handle { get; } = handle;

    /// <summary> The end handle of the attribute group </summary>
    public ushort EndGroupHandle { get; } = endGroupHandle;

    /// <inheritdoc />
    public BleUuid AttributeType => _attribute.AttributeType;

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckReadPermissions(clientPeer);

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
    public bool Equals(GattDatabaseGroupEntry other)
    {
        return _attribute.AttributeType.Equals(other._attribute.AttributeType)
            && Handle == other.Handle
            && EndGroupHandle == other.EndGroupHandle;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GattDatabaseGroupEntry entry && Equals(entry);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_attribute.AttributeType, Handle);

    /// <inheritdoc />
    public static bool operator ==(GattDatabaseGroupEntry left, GattDatabaseGroupEntry right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(GattDatabaseGroupEntry left, GattDatabaseGroupEntry right) => !(left == right);
}
