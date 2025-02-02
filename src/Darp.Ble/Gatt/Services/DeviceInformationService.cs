using System.Buffers.Binary;
using System.Text;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gatt.Services;

/// <summary> The SystemId </summary>
/// <param name="ManufacturerDefinedIdentifier"> The 40-bit manufacturer defined identifier </param>
/// <param name="OrganizationallyUniqueIdentifier"> The 24 bit organizational unique identifier </param>
public readonly record struct SystemId(ulong ManufacturerDefinedIdentifier, uint OrganizationallyUniqueIdentifier)
{
    /// <summary> Read the system id from a span in little endian format </summary>
    /// <param name="source"> The source to read from </param>
    /// <returns> The resulting system id </returns>
    public static SystemId ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        ulong systemId = BinaryPrimitives.ReadUInt64LittleEndian(source);
        var oui = (uint)(systemId >> 40);
        ulong mdi = systemId & 0xFFFFFFFFFF;
        return new SystemId(mdi, oui);
    }

    /// <summary> Create a byte array in little endian format </summary>
    /// <returns> The resulting byte array </returns>
    public byte[] ToByteArrayLittleEndian()
    {
        ulong systemId = ((ulong)OrganizationallyUniqueIdentifier << 40) | ManufacturerDefinedIdentifier;

        var buffer = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, systemId);
        return buffer;
    }
}

/// <summary> The service contract for a device information service </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/dis-1-2/"/>
public static class DeviceInformationServiceContract
{
    /// <summary> The uuid of the service </summary>
    public static ServiceDeclaration DeviceInformationService => new(0x180A);

    /// <summary> The system id characteristic </summary>
    public static TypedCharacteristicDeclaration<SystemId, Properties.Read> SystemIdCharacteristic { get; } =
        CharacteristicDeclaration.Create<SystemId, Properties.Read>(0x2A23, SystemId.ReadLittleEndian, id => id.ToByteArrayLittleEndian());
    /// <summary> The manufacturer name characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> ModelNumberCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A24, Encoding.UTF8);
    /// <summary> The model number characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> SerialNumberCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A25, Encoding.UTF8);
    /// <summary> The serial number characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> FirmwareRevisionCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A26, Encoding.UTF8);
    /// <summary> The hardware revision characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> HardwareRevisionCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A27, Encoding.UTF8);
    /// <summary> The firmware revision characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> SoftwareRevisionCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A28, Encoding.UTF8);
    /// <summary> The software revision characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> ManufacturerNameCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A29, Encoding.UTF8);
    /// <summary> The regulatory certification data list characteristic </summary>
    public static CharacteristicDeclaration<Properties.Read> RegulatoryCertificationDataCharacteristic { get; }
        = new(0x2A2A);
    /// <summary> The regulatory certification data list characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> PnPIdCharacteristic { get; }
        = CharacteristicDeclaration.Create<Properties.Read>(0x2A50, Encoding.UTF8);

    /// <summary> Add a new DeviceInformationService to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="manufacturerName"> The Manufacturer Name String characteristic shall represent the name of the manufacturer of the device </param>
    /// <param name="modelNumber"> The Model Number String characteristic shall represent the model number that is assigned by the device vendor. </param>
    /// <param name="serialNumber"> The Serial Number String characteristic shall represent the serial number for a particular instance of the device. </param>
    /// <param name="hardwareRevision"> The Hardware Revision String characteristic shall represent the hardware revision for the hardware within the device. </param>
    /// <param name="firmwareRevision"> The Firmware Revision String characteristic shall represent the firmware revision for the firmware within the device. </param>
    /// <param name="softwareRevision"> The Software Revision String characteristic shall represent the software revision for the software within the device. </param>
    /// <param name="systemId"> The System ID characteristic shall represent a structure containing an Organizationally Unique Identifier (OUI) followed by a manufacturer-defined identifier and is unique for each individual instance of the product. </param>
    /// <param name="ieeeRegulatoryCertificationData"> The IEEE 11073-20601 Regulatory Certification Data List characteristic shall represent regulatory and certification information for the product in a list defined in IEEE 11073-20601 </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which contains the service on completion </returns>
    public static async Task<GattClientDeviceInformationService> AddDeviceInformationServiceAsync(
        this IBlePeripheral peripheral,
        string? manufacturerName = null,
        string? modelNumber = null,
        string? serialNumber = null,
        string? hardwareRevision = null,
        string? firmwareRevision = null,
        string? softwareRevision = null,
        SystemId? systemId = null,
        byte[]? ieeeRegulatoryCertificationData = null,
        CancellationToken cancellationToken = default
        )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(
            DeviceInformationService,
            cancellationToken
            ).ConfigureAwait(false);

        // Add optional manufacturer name characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? manufacturerNameCharacteristic = null;
        if (manufacturerName is not null)
        {
            manufacturerNameCharacteristic = await service
                .AddCharacteristicAsync(ManufacturerNameCharacteristic, manufacturerName, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional model number characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? modelNumberCharacteristic = null;
        if (modelNumber is not null)
        {
            modelNumberCharacteristic = await service
                .AddCharacteristicAsync(ModelNumberCharacteristic, modelNumber, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? serialNumberCharacteristic = null;
        if (serialNumber is not null)
        {
            serialNumberCharacteristic = await service
                .AddCharacteristicAsync(SerialNumberCharacteristic, serialNumber, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? hardwareRevisionCharacteristic = null;
        if (hardwareRevision is not null)
        {
            hardwareRevisionCharacteristic = await service
                .AddCharacteristicAsync(HardwareRevisionCharacteristic, hardwareRevision, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? firmwareRevisionCharacteristic = null;
        if (firmwareRevision is not null)
        {
            firmwareRevisionCharacteristic = await service
                .AddCharacteristicAsync(FirmwareRevisionCharacteristic, firmwareRevision, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattTypedClientCharacteristic<string, Properties.Read>? softwareRevisionCharacteristic = null;
        if (softwareRevision is not null)
        {
            softwareRevisionCharacteristic = await service
                .AddCharacteristicAsync(SoftwareRevisionCharacteristic, softwareRevision, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattTypedClientCharacteristic<SystemId, Properties.Read>? systemIdCharacteristic = null;
        if (systemId is not null)
        {
            systemIdCharacteristic = await service
                .AddCharacteristicAsync(SystemIdCharacteristic, systemId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        // Add optional serial number characteristic
        GattClientCharacteristic<Properties.Read>? regulatoryCertificationDataCharacteristic = null;
        if (ieeeRegulatoryCertificationData is not null)
        {
            regulatoryCertificationDataCharacteristic = await service
                .AddCharacteristicAsync<Properties.Read>(RegulatoryCertificationDataCharacteristic.Uuid, ieeeRegulatoryCertificationData, cancellationToken)
                .ConfigureAwait(false);
        }

        return new GattClientDeviceInformationService(service)
        {
            ManufacturerName = manufacturerNameCharacteristic,
            ModelNumber = modelNumberCharacteristic,
            SerialNumberCharacteristic = serialNumberCharacteristic,
            HardwareRevisionCharacteristic = hardwareRevisionCharacteristic,
            FirmwareRevisionCharacteristic = firmwareRevisionCharacteristic,
            SoftwareRevisionCharacteristic = softwareRevisionCharacteristic,
            SystemIdCharacteristic = systemIdCharacteristic,
            RegulatoryCertificationDataCharacteristic = regulatoryCertificationDataCharacteristic,
        };
    }

    /// <summary> Discover the device information service from the given peer gatt server </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerDeviceInformationService> DiscoverDeviceInformationServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer.DiscoverServiceAsync(DeviceInformationService, cancellationToken).ConfigureAwait(false);

        // Discover the characteristics
        await service.DiscoverCharacteristicsAsync(cancellationToken).ConfigureAwait(false);
        service.TryGetCharacteristic(ManufacturerNameCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? manufacturerNameCharacteristic);
        service.TryGetCharacteristic(ModelNumberCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? modelNumberCharacteristic);
        service.TryGetCharacteristic(SerialNumberCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? serialNumberCharacteristic);
        service.TryGetCharacteristic(HardwareRevisionCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? hardwareRevisionCharacteristic);
        service.TryGetCharacteristic(FirmwareRevisionCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? firmwareRevisionCharacteristic);
        service.TryGetCharacteristic(SoftwareRevisionCharacteristic, out TypedGattServerCharacteristic<string, Properties.Read>? softwareRevisionCharacteristic);
        service.TryGetCharacteristic(SystemIdCharacteristic, out TypedGattServerCharacteristic<SystemId, Properties.Read>? systemIdCharacteristic);
        service.TryGetCharacteristic(RegulatoryCertificationDataCharacteristic, out GattServerCharacteristic<Properties.Read>? regulatoryCertificationDataCharacteristic);

        return new GattServerDeviceInformationService(service)
        {
            ManufacturerName = manufacturerNameCharacteristic,
            ModelNumber = modelNumberCharacteristic,
            SerialNumber = serialNumberCharacteristic,
            HardwareRevision = hardwareRevisionCharacteristic,
            FirmwareRevision = firmwareRevisionCharacteristic,
            SoftwareRevision = softwareRevisionCharacteristic,
            SystemId = systemIdCharacteristic,
            RegulatoryCertificationData = regulatoryCertificationDataCharacteristic,
        };
    }
}

/// <summary> The DeviceInformationService wrapper representing the gatt client </summary>
public sealed class GattClientDeviceInformationService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> The manufacturer name characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? ManufacturerName { get; init; }
    /// <summary> The model number characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? ModelNumber { get; init; }
    /// <summary> The serial number characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? SerialNumberCharacteristic { get; init; }
    /// <summary> The hardware revision characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? HardwareRevisionCharacteristic { get; init; }
    /// <summary> The firmware revision characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? FirmwareRevisionCharacteristic { get; init; }
    /// <summary> The software revision characteristic </summary>
    public required GattTypedClientCharacteristic<string, Properties.Read>? SoftwareRevisionCharacteristic { get; init; }
    /// <summary> The system id characteristic </summary>
    public required GattTypedClientCharacteristic<SystemId, Properties.Read>? SystemIdCharacteristic { get; init; }
    /// <summary> The regulatory certification data list characteristic </summary>
    public required GattClientCharacteristic<Properties.Read>? RegulatoryCertificationDataCharacteristic { get; init; }
}

/// <summary> The DeviceInformationService wrapper representing the gatt server </summary>
public sealed class GattServerDeviceInformationService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> The manufacturer name characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? ManufacturerName { get; init; }
    /// <summary> The model number characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? ModelNumber { get; init; }
    /// <summary> The serial number characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? SerialNumber { get; init; }
    /// <summary> The hardware revision characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? HardwareRevision { get; init; }
    /// <summary> The firmware revision characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? FirmwareRevision { get; init; }
    /// <summary> The software revision characteristic </summary>
    public required TypedGattServerCharacteristic<string, Properties.Read>? SoftwareRevision { get; init; }
    /// <summary> The system id characteristic </summary>
    public required TypedGattServerCharacteristic<SystemId, Properties.Read>? SystemId { get; init; }
    /// <summary> The regulatory certification data list characteristic </summary>
    public required GattServerCharacteristic<Properties.Read>? RegulatoryCertificationData { get; init; }
}