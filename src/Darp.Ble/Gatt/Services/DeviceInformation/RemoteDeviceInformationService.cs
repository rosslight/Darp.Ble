namespace Darp.Ble.Gatt.Services;

/// <summary> The device information service of a connected peripheral </summary>
/// <param name="ManufacturerName"> The manufacturer name, when present </param>
/// <param name="ModelNumber"> The model number, when present </param>
/// <param name="SerialNumber"> The serial number, when present </param>
/// <param name="HardwareRevision"> The hardware revision, when present </param>
/// <param name="FirmwareRevision"> The firmware revision, when present </param>
/// <param name="SoftwareRevision"> The software revision, when present </param>
/// <param name="SystemId"> The system ID, when present </param>
/// <param name="RegulatoryCertificationData"> The regulatory certification data, when present </param>
/// <param name="PnPId"> The PnP ID, when present </param>
public sealed record RemoteDeviceInformationService(
    TypedRemoteCharacteristic<string, Properties.Read>? ManufacturerName,
    TypedRemoteCharacteristic<string, Properties.Read>? ModelNumber,
    TypedRemoteCharacteristic<string, Properties.Read>? SerialNumber,
    TypedRemoteCharacteristic<string, Properties.Read>? HardwareRevision,
    TypedRemoteCharacteristic<string, Properties.Read>? FirmwareRevision,
    TypedRemoteCharacteristic<string, Properties.Read>? SoftwareRevision,
    TypedRemoteCharacteristic<SystemId, Properties.Read>? SystemId,
    RemoteCharacteristic<Properties.Read>? RegulatoryCertificationData,
    RemoteCharacteristic<Properties.Read>? PnPId
);
