using System.Text;
using Darp.Ble.Backends;

namespace Darp.Ble.Gatt;

/// <summary> Descriptors every characteristic can have </summary>
public static class LocalGattCharacteristicExtensions
{
    /// <summary> The UUID of the characteristic user description descriptor </summary>
    private const ushort UserDescriptionUuid = 0x2901;

    /// <summary> Adds a read-only user description, while the service is being built </summary>
    /// <remarks> Unlike in v1, centrals cannot change it </remarks>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="description"> The description, sent as UTF-8 </param>
    /// <returns> The descriptor </returns>
    /// <exception cref="InvalidOperationException"> The service was already added </exception>
    public static LocalGattDescriptor AddUserDescription(
        this LocalGattCharacteristic characteristic,
        string description
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(description);
        byte[] value = Encoding.UTF8.GetBytes(description);
        return characteristic.AddDescriptor(
            new GattDescriptorDefinition(UserDescriptionUuid, value),
            onRead: (_, _) => ValueTask.FromResult(value),
            onWrite: null
        );
    }
}
