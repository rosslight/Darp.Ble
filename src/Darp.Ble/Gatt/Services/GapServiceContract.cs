using System.Buffers.Binary;
using System.Text;
using Darp.Ble.Connections;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gatt.Services;

/// <summary> The Generic Access service. The backend serves it; set <see cref="BlePeripheral.DeviceName"/> and <see cref="BlePeripheral.Appearance"/> to change it </summary>
/// <seealso href="https://www.bluetooth.com/specifications/assigned-numbers/"/>
public static class GapServiceContract
{
    /// <summary> The declaration of the service, 0x1800 </summary>
    public static ServiceDeclaration GapService { get; } = new(0x1800);

    /// <summary> The device name, 0x2A00, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> DeviceNameCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A00, Encoding.UTF8);

    /// <summary> The appearance, 0x2A01, a little-endian 16-bit value </summary>
    public static TypedCharacteristicDeclaration<AppearanceValues, Properties.Read> AppearanceCharacteristic { get; } =
        CharacteristicDeclaration.Create<AppearanceValues, Properties.Read>(
            0x2A01,
            bytes => (AppearanceValues)BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            value =>
            {
                var bytes = new byte[2];
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)value);
                return bytes;
            }
        );

    /// <summary> Discovers the GAP service of a connected peripheral </summary>
    /// <param name="gatt"> The peripheral's services </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service's characteristics </returns>
    public static async Task<RemoteGapService> DiscoverGapServiceAsync(
        this GattClient gatt,
        CancellationToken cancellationToken = default
    )
    {
        RemoteGattService service = await gatt.DiscoverServiceAsync(GapService, cancellationToken)
            .ConfigureAwait(false);
        return new RemoteGapService(
            await service
                .DiscoverCharacteristicAsync(DeviceNameCharacteristic, cancellationToken)
                .ConfigureAwait(false),
            await service.DiscoverCharacteristicAsync(AppearanceCharacteristic, cancellationToken).ConfigureAwait(false)
        );
    }
}
