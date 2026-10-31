using System.Text;
using Darp.Ble.Connections;

namespace Darp.Ble.Gatt.Services;

/// <summary> The Device Information service; every characteristic is optional </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/device-information-service-1-1/"/>
public static class DeviceInformationServiceContract
{
    /// <summary> The declaration of the service, 0x180A </summary>
    public static ServiceDeclaration DeviceInformationService { get; } = new(0x180A);

    /// <summary> The manufacturer name, 0x2A29, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> ManufacturerNameCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A29, Encoding.UTF8);

    /// <summary> The model number, 0x2A24, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> ModelNumberCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A24, Encoding.UTF8);

    /// <summary> The serial number, 0x2A25, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> SerialNumberCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A25, Encoding.UTF8);

    /// <summary> The hardware revision, 0x2A27, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> HardwareRevisionCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A27, Encoding.UTF8);

    /// <summary> The firmware revision, 0x2A26, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> FirmwareRevisionCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A26, Encoding.UTF8);

    /// <summary> The software revision, 0x2A28, as UTF-8 </summary>
    public static TypedCharacteristicDeclaration<string, Properties.Read> SoftwareRevisionCharacteristic { get; } =
        CharacteristicDeclaration.Create<Properties.Read>(0x2A28, Encoding.UTF8);

    /// <summary> The system ID, 0x2A23 </summary>
    public static TypedCharacteristicDeclaration<SystemId, Properties.Read> SystemIdCharacteristic { get; } =
        CharacteristicDeclaration.Create<SystemId, Properties.Read>(
            0x2A23,
            SystemId.ReadLittleEndian,
            id => id.ToByteArrayLittleEndian()
        );

    /// <summary> The IEEE 11073-20601 regulatory certification data list, 0x2A2A, as raw bytes </summary>
    public static CharacteristicDeclaration<Properties.Read> RegulatoryCertificationDataCharacteristic { get; } =
        new(0x2A2A);

    /// <summary> The PnP ID, 0x2A50, as raw bytes: vendor ID source, vendor ID, product ID and product version </summary>
    public static CharacteristicDeclaration<Properties.Read> PnPIdCharacteristic { get; } = new(0x2A50);

    /// <summary> Adds a device information service with the given values </summary>
    /// <param name="peripheral"> The adapter's peripheral role </param>
    /// <param name="manufacturerName"> The manufacturer name; left out when null </param>
    /// <param name="modelNumber"> The model number; left out when null </param>
    /// <param name="serialNumber"> The serial number; left out when null </param>
    /// <param name="hardwareRevision"> The hardware revision; left out when null </param>
    /// <param name="firmwareRevision"> The firmware revision; left out when null </param>
    /// <param name="softwareRevision"> The software revision; left out when null </param>
    /// <param name="systemId"> The system ID; left out when null </param>
    /// <param name="regulatoryCertificationData"> The regulatory certification data, copied; left out when null </param>
    /// <param name="pnpId"> The PnP ID, copied; left out when null </param>
    /// <param name="cancellationToken"> Cancels adding </param>
    /// <returns> The service </returns>
    public static async Task<LocalDeviceInformationService> AddDeviceInformationServiceAsync(
        this BlePeripheral peripheral,
        string? manufacturerName = null,
        string? modelNumber = null,
        string? serialNumber = null,
        string? hardwareRevision = null,
        string? firmwareRevision = null,
        string? softwareRevision = null,
        SystemId? systemId = null,
        byte[]? regulatoryCertificationData = null,
        byte[]? pnpId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        var builder = new LocalGattServiceBuilder(DeviceInformationService);
        TypedLocalCharacteristic<string, Properties.Read>? manufacturerNameCharacteristic = AddString(
            builder,
            ManufacturerNameCharacteristic,
            manufacturerName
        );
        TypedLocalCharacteristic<string, Properties.Read>? modelNumberCharacteristic = AddString(
            builder,
            ModelNumberCharacteristic,
            modelNumber
        );
        TypedLocalCharacteristic<string, Properties.Read>? serialNumberCharacteristic = AddString(
            builder,
            SerialNumberCharacteristic,
            serialNumber
        );
        TypedLocalCharacteristic<string, Properties.Read>? hardwareRevisionCharacteristic = AddString(
            builder,
            HardwareRevisionCharacteristic,
            hardwareRevision
        );
        TypedLocalCharacteristic<string, Properties.Read>? firmwareRevisionCharacteristic = AddString(
            builder,
            FirmwareRevisionCharacteristic,
            firmwareRevision
        );
        TypedLocalCharacteristic<string, Properties.Read>? softwareRevisionCharacteristic = AddString(
            builder,
            SoftwareRevisionCharacteristic,
            softwareRevision
        );
        TypedLocalCharacteristic<SystemId, Properties.Read>? systemIdCharacteristic = systemId is { } id
            ? builder.AddCharacteristic(SystemIdCharacteristic, onRead: (_, _) => ValueTask.FromResult(id))
            : null;
        LocalCharacteristic<Properties.Read>? regulatoryCharacteristic = AddBytes(
            builder,
            RegulatoryCertificationDataCharacteristic,
            regulatoryCertificationData
        );
        LocalCharacteristic<Properties.Read>? pnpIdCharacteristic = AddBytes(builder, PnPIdCharacteristic, pnpId);
        LocalGattService service = await peripheral.AddServiceAsync(builder, cancellationToken).ConfigureAwait(false);
        return new LocalDeviceInformationService(
            service,
            manufacturerNameCharacteristic,
            modelNumberCharacteristic,
            serialNumberCharacteristic,
            hardwareRevisionCharacteristic,
            firmwareRevisionCharacteristic,
            softwareRevisionCharacteristic,
            systemIdCharacteristic,
            regulatoryCharacteristic,
            pnpIdCharacteristic
        );
    }

    /// <summary> Discovers the device information service of a connected peripheral </summary>
    /// <param name="connection"> The connection to the peripheral </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service's characteristics; each is null when the peripheral lacks it </returns>
    public static async Task<RemoteDeviceInformationService> DiscoverDeviceInformationServiceAsync(
        this ConnectedPeripheral connection,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        RemoteGattService service = await connection
            .DiscoverServiceAsync(DeviceInformationService, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service
            .DiscoverCharacteristicsAsync(cancellationToken)
            .ConfigureAwait(false);
        RemoteGattCharacteristic? systemId = RemoteGattServiceExtensions.Match(characteristics, SystemIdCharacteristic);
        RemoteGattCharacteristic? regulatory = RemoteGattServiceExtensions.Match(
            characteristics,
            RegulatoryCertificationDataCharacteristic
        );
        RemoteGattCharacteristic? pnpId = RemoteGattServiceExtensions.Match(characteristics, PnPIdCharacteristic);
        return new RemoteDeviceInformationService(
            Typed(characteristics, ManufacturerNameCharacteristic),
            Typed(characteristics, ModelNumberCharacteristic),
            Typed(characteristics, SerialNumberCharacteristic),
            Typed(characteristics, HardwareRevisionCharacteristic),
            Typed(characteristics, FirmwareRevisionCharacteristic),
            Typed(characteristics, SoftwareRevisionCharacteristic),
            systemId is null
                ? null
                : new TypedRemoteCharacteristic<SystemId, Properties.Read>(systemId, SystemIdCharacteristic),
            regulatory is null ? null : new RemoteCharacteristic<Properties.Read>(regulatory),
            pnpId is null ? null : new RemoteCharacteristic<Properties.Read>(pnpId)
        );
    }

    /// <summary> Adds a read-only raw characteristic with a snapshot of the value, so later changes to the caller's array do not leak </summary>
    private static LocalCharacteristic<Properties.Read>? AddBytes(
        LocalGattServiceBuilder builder,
        CharacteristicDeclaration<Properties.Read> declaration,
        byte[]? value
    )
    {
        if (value is null)
            return null;
        byte[] snapshot = [.. value];
        return builder.AddCharacteristic(declaration, onRead: (_, _) => ValueTask.FromResult(snapshot));
    }

    private static TypedLocalCharacteristic<string, Properties.Read>? AddString(
        LocalGattServiceBuilder builder,
        TypedCharacteristicDeclaration<string, Properties.Read> declaration,
        string? value
    ) => value is null ? null : builder.AddCharacteristic(declaration, onRead: (_, _) => ValueTask.FromResult(value));

    private static TypedRemoteCharacteristic<string, Properties.Read>? Typed(
        IReadOnlyList<RemoteGattCharacteristic> characteristics,
        TypedCharacteristicDeclaration<string, Properties.Read> declaration
    ) =>
        RemoteGattServiceExtensions.Match(characteristics, declaration) is { } characteristic
            ? new TypedRemoteCharacteristic<string, Properties.Read>(characteristic, declaration)
            : null;
}
