using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

/// <summary> Gatt descriptor extensions </summary>
public static class GattDescriptorExtensions
{
    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to be added </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    [OverloadResolutionPriority(1)]
    public static IGattClientDescriptor AddDescriptor(
        this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        IGattAttribute.OnReadAsyncCallback? onRead = null,
        IGattAttribute.OnWriteAsyncCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.AddDescriptor(
            new FuncCharacteristicValue(
                uuid,
                characteristic.Service.Peripheral.GattDatabase,
                onRead.CreateReadAccessPermissionFunc(),
                onRead,
                onWrite.CreateWriteAccessPermissionFunc(),
                onWrite
            )
        );
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to be added </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static IGattClientDescriptor AddDescriptor(
        this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        IGattAttribute.OnReadCallback? onRead = null,
        IGattAttribute.OnWriteCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattAttribute.OnReadAsyncCallback? onAsyncRead = onRead is null
            ? null
            : peer => ValueTask.FromResult(onRead(peer));
        IGattAttribute.OnWriteAsyncCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes) => ValueTask.FromResult(onWrite(peer, bytes));
        return characteristic.AddDescriptor(uuid, onAsyncRead, onAsyncWrite);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static IGattClientDescriptor AddDescriptor(
        this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        IGattAttribute.OnReadCallback? onRead = null,
        IGattAttribute.OnWriteCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptor(descriptorDeclaration.Uuid, onRead, onWrite);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static IGattClientDescriptor AddDescriptor(
        this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        byte[] staticValue
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptor(descriptorDeclaration.Uuid, staticValue);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to add </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static IGattClientDescriptor AddDescriptor(
        this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        byte[] staticValue
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.AddDescriptor(
            uuid,
            _ => ValueTask.FromResult(staticValue),
            (_, value) =>
            {
                staticValue = value.ToArray();
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
    }

    /// <summary> Add a user description to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add the descriptor to </param>
    /// <param name="description"> The description to add </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static IGattClientDescriptor AddUserDescription(
        this IGattClientCharacteristic characteristic,
        string description
    )
    {
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);
        return characteristic.AddDescriptor(DescriptorDeclaration.CharacteristicUserDescription, descriptionBytes);
    }
}
