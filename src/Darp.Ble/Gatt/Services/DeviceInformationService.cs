using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Services;

public enum VendorIdSource : byte
{
    BluetoothSigAssigned = 0x01,
    USBImplementersForumAssigned = 0x02,
}

public readonly record struct PnP(VendorIdSource VendorIdSource, ushort VendorId, ushort ProductId, ushort ProductVersion);

public static class DeviceInformationServiceContract
{
    public static BleUuid Uuid => new(0x180A);
    public static Characteristic<Properties.Read> ManufacturerNameCharacteristic { get; } = new(0x2A29);

    public static async Task<GattClientDeviceInformationService> AddDeviceInformationServiceAsync(
        this IBlePeripheral peripheral,
        string? manufacturerName = null,
        string? modelNumber = null,
        string? serialNumber = null,
        string? hardwareRevision = null,
        string? firmwareRevision = null,
        string? softwareRevision = null,
        (int, int)? systemId = null,
        byte[]? ieeeRegulatoryCertificationData = null,
        CancellationToken cancellationToken = default
        )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(
            Uuid,
            cancellationToken
            ).ConfigureAwait(false);

        // Add optional manufacturer name characteristic
        GattClientCharacteristic<Properties.Read>? manufacturerNameCharacteristic = null;
        if (manufacturerName is not null)
        {
            manufacturerNameCharacteristic = await service.AddCharacteristicAsync<Properties.Read>(
                    ManufacturerNameCharacteristic.Uuid,
                    Encoding.UTF8.GetBytes(manufacturerName),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional manufacturer name characteristic
        GattClientCharacteristic<Properties.Read>? modelNumberCharacteristic = null;
        if (modelNumber is not null)
        {
            modelNumberCharacteristic = await service.AddCharacteristicAsync<Properties.Read>(
                    ManufacturerNameCharacteristic.Uuid,
                    Encoding.UTF8.GetBytes(modelNumber),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new GattClientDeviceInformationService
        {
            ManufacturerName = manufacturerNameCharacteristic,
            ModelNumber = modelNumberCharacteristic,
        };
    }

    public sealed class GattClientDeviceInformationService
    {
        public required GattClientCharacteristic<Properties.Read>? ManufacturerName { get; init; }
        public required GattClientCharacteristic<Properties.Read>? ModelNumber { get; init; }
    }
}
