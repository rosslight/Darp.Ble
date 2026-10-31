namespace Darp.Ble.Gatt.Services;

/// <summary> A device information service the adapter hosts </summary>
/// <param name="Service"> The hosted service, e.g. to advertise its UUID </param>
/// <param name="ManufacturerName"> The manufacturer name, when present </param>
/// <param name="ModelNumber"> The model number, when present </param>
/// <param name="SerialNumber"> The serial number, when present </param>
/// <param name="HardwareRevision"> The hardware revision, when present </param>
/// <param name="FirmwareRevision"> The firmware revision, when present </param>
/// <param name="SoftwareRevision"> The software revision, when present </param>
/// <param name="SystemId"> The system ID, when present </param>
/// <param name="RegulatoryCertificationData"> The regulatory certification data, when present </param>
/// <param name="PnPId"> The PnP ID, when present </param>
public sealed record LocalDeviceInformationService(
    LocalGattService Service,
    TypedLocalCharacteristic<string, Properties.Read>? ManufacturerName,
    TypedLocalCharacteristic<string, Properties.Read>? ModelNumber,
    TypedLocalCharacteristic<string, Properties.Read>? SerialNumber,
    TypedLocalCharacteristic<string, Properties.Read>? HardwareRevision,
    TypedLocalCharacteristic<string, Properties.Read>? FirmwareRevision,
    TypedLocalCharacteristic<string, Properties.Read>? SoftwareRevision,
    TypedLocalCharacteristic<SystemId, Properties.Read>? SystemId,
    LocalCharacteristic<Properties.Read>? RegulatoryCertificationData,
    LocalCharacteristic<Properties.Read>? PnPId
);
