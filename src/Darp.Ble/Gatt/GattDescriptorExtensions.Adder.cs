using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
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
    [OverloadResolutionPriority(1)]
    public static void AddDescriptor(
        this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        OnReadAsyncCallback? onRead = null,
        OnWriteAsyncCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.AddDescriptor(
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
    public static void AddDescriptor(
        this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        OnReadCallback? onRead = null,
        OnWriteCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        OnReadAsyncCallback? onAsyncRead = onRead is null ? null : peer => ValueTask.FromResult(onRead(peer));
        OnWriteAsyncCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes) => ValueTask.FromResult(onWrite(peer, bytes));
        characteristic.AddDescriptor(uuid, onAsyncRead, onAsyncWrite);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    public static void AddDescriptor(
        this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        OnReadCallback? onRead = null,
        OnWriteCallback? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        characteristic.AddDescriptor(descriptorDeclaration.Uuid, onRead, onWrite);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    public static void AddDescriptor(
        this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        byte[] staticValue
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        characteristic.AddDescriptor(descriptorDeclaration.Uuid, staticValue);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to add </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    public static void AddDescriptor(this IGattClientCharacteristic characteristic, BleUuid uuid, byte[] staticValue)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.AddDescriptor(
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
    public static void AddUserDescription(this IGattClientCharacteristic characteristic, string description)
    {
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);
        characteristic.AddDescriptor(DescriptorDeclaration.CharacteristicUserDescription, descriptionBytes);
    }

    /// <summary> Add a new client characteristic configuration descriptor </summary>
    /// <param name="characteristic"> The characteristic to add the descriptor to </param>
    public static void AddClientCharacteristicConfiguration(this IGattClientCharacteristic characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);

        var dictionary = new ConcurrentDictionary<IGattClientPeer, byte[]>();
        characteristic.Service.Peripheral.WhenDisconnected.Subscribe(peer => dictionary.Remove(peer, out _));

        characteristic.AddDescriptor(
            DescriptorDeclaration.ClientCharacteristicConfiguration,
            onRead: peer => peer is not null && dictionary.TryGetValue(peer, out byte[]? value) ? value : [0x00, 0x00],
            onWrite: (peer, value) =>
            {
                if (peer is null || value.Length != 2)
                    return GattProtocolStatus.OutOfRange;
                dictionary.AddOrUpdate(
                    peer,
                    static (_, newValue) => newValue,
                    static (_, _, newValue) => newValue,
                    value
                );
                return GattProtocolStatus.Success;
            }
        );
    }
}
