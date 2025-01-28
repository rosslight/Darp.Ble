using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

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
    public static TypedCharacteristic<string, Properties.Read> ManufacturerNameCharacteristic { get; } =
        Characteristic.Create<Properties.Read>(0x2A29, Encoding.UTF8);
    public static TypedCharacteristic<string, Properties.Read> ModelNumberCharacteristic { get; } =
        Characteristic.Create<Properties.Read>(0x2A24, Encoding.UTF8);

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
        GattTypedClientCharacteristic<string, Properties.Read>? manufacturerNameCharacteristic = null;
        if (manufacturerName is not null)
        {
            manufacturerNameCharacteristic = await service.AddCharacteristicAsync(
                    ManufacturerNameCharacteristic,
                    manufacturerName,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional manufacturer name characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? modelNumberCharacteristic = null;
        if (modelNumber is not null)
        {
            modelNumberCharacteristic = await service.AddCharacteristicAsync(
                    ModelNumberCharacteristic,
                    modelNumber,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        return new GattClientDeviceInformationService
        {
            ManufacturerName = manufacturerNameCharacteristic,
            ModelNumber = modelNumberCharacteristic,
        };
    }

    public static async Task<GattServerDeviceInformationService> DiscoverEchoServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer.DiscoverServiceAsync(Uuid, cancellationToken).ConfigureAwait(false);

        // Discover the characteristics
        await service.DiscoverCharacteristicAsync(cancellationToken).ConfigureAwait(false);
        service.TryGetCharacteristic(ManufacturerNameCharacteristic, out IGattServerCharacteristic<string, Properties.Read>? manufacturerNameCharacteristic);
        service.TryGetCharacteristic(ModelNumberCharacteristic, out IGattServerCharacteristic<string, Properties.Read>? modelNumberCharacteristic);

        return new GattServerDeviceInformationService
        {
            ManufacturerName = manufacturerNameCharacteristic,
            ModelNumber = modelNumberCharacteristic,
        };
    }

    public sealed class GattClientDeviceInformationService
    {
        public required GattTypedClientCharacteristic<string, Properties.Read>? ManufacturerName { get; init; }
        public required GattTypedClientCharacteristic<string, Properties.Read>? ModelNumber { get; init; }
    }

    public sealed class GattServerDeviceInformationService
    {
        public required IGattServerCharacteristic<string, Properties.Read>? ManufacturerName { get; init; }
        public required IGattServerCharacteristic<string, Properties.Read>? ModelNumber { get; init; }
    }
}
