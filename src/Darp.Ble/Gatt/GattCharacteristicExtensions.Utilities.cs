using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    private static byte[] GetValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer)
    {
        ValueTask<byte[]> getTask = characteristic.GetValueAsync(clientPeer, CancellationToken.None);
        return getTask.IsCompleted ? getTask.Result : getTask.AsTask().GetAwaiter().GetResult();
    }

    private static GattProtocolStatus UpdateValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer, byte[] value)
    {
        ValueTask<GattProtocolStatus> updateTask = characteristic.UpdateValueAsync(clientPeer, value, CancellationToken.None);
        return updateTask.IsCompleted ? updateTask.Result : updateTask.AsTask().GetAwaiter().GetResult();
    }

    private static void StartUpdateValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer, byte[] value)
    {
        ValueTask<GattProtocolStatus> updateTask = characteristic.UpdateValueAsync(clientPeer, value, CancellationToken.None);
        if (updateTask.IsCompleted)
            return;
        _ = updateTask.AsTask();
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to be added </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        Func<IGattClientPeer?, byte[]>? onRead = null,
        Func<IGattClientPeer?, byte[], GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientAttribute.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : (peer, _) => ValueTask.FromResult(onRead(peer));
        IGattClientAttribute.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, _) => ValueTask.FromResult(onWrite(peer, bytes));
        return characteristic.AddDescriptorAsync(uuid, onAsyncRead, onAsyncWrite, cancellationToken);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    [OverloadResolutionPriority(1)]
    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        IGattClientAttribute.OnReadCallback? onRead = null,
        IGattClientAttribute.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptorAsync(descriptorDeclaration.Uuid, onRead, onWrite, cancellationToken);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        Func<IGattClientPeer?, byte[]>? onRead = null,
        Func<IGattClientPeer?, byte[], GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptorAsync(descriptorDeclaration.Uuid, onRead, onWrite, cancellationToken);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="descriptorDeclaration"> The descriptor declaration </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        byte[] staticValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptorAsync(descriptorDeclaration.Uuid, staticValue, cancellationToken);
    }

    /// <summary> Add a descriptor to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add to </param>
    /// <param name="uuid"> The uuid of the descriptor to add </param>
    /// <param name="staticValue"> The static value of the characteristic </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        BleUuid uuid,
        byte[] staticValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.AddDescriptorAsync(uuid,
            (_, _) => ValueTask.FromResult(staticValue),
            (_, value, _) =>
            {
                staticValue = value;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken);
    }

    /// <summary> Add a user description to the characteristic </summary>
    /// <param name="characteristic"> The characteristic to add the descriptor to </param>
    /// <param name="description"> The description to add </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    public static async Task<IGattClientDescriptor> AddUserDescriptionAsync(this IGattClientCharacteristic characteristic,
        string description,
        CancellationToken cancellationToken)
    {
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);
        return await characteristic
            .AddDescriptorAsync(DescriptorDeclaration.CharacteristicUserDescription, descriptionBytes, cancellationToken)
            .ConfigureAwait(false);
    }
}