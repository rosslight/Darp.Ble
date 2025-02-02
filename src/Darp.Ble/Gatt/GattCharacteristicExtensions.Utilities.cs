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

    public static Task<IGattClientDescriptor> AddDescriptorAsync(this IGattClientCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        byte[] staticValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.AddDescriptorAsync(descriptorDeclaration.Uuid, staticValue, cancellationToken);
    }

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

    public static async Task<IGattClientDescriptor> AddUserDescriptionAsync(this IGattClientCharacteristic characteristic,
        string description,
        CancellationToken cancellationToken)
    {
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);
        return await characteristic
            .AddDescriptorAsync(DescriptorDeclaration.CharacteristicUserDescription,
                descriptionBytes,
                cancellationToken)
            .ConfigureAwait(false);
    }
}