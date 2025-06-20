namespace Darp.Ble.Data.AssignedNumbers;

/// <summary>
/// The AdTypes.
/// This enum was autogenerated on 01/03/2025
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming")]
public enum AdTypes : byte
{
    /// <summary> Flags </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.3 </remarks>
    Flags = 0x01,

    /// <summary> Incomplete List of 16-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteListOf16BitServiceOrServiceClassUuids = 0x02,

    /// <summary> Complete List of 16-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteListOf16BitServiceOrServiceClassUuids = 0x03,

    /// <summary> Incomplete List of 32-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteListOf32BitServiceOrServiceClassUuids = 0x04,

    /// <summary> Complete List of 32-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteListOf32BitServiceOrServiceClassUuids = 0x05,

    /// <summary> Incomplete List of 128-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteListOf128BitServiceOrServiceClassUuids = 0x06,

    /// <summary> Complete List of 128-bit Service or Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteListOf128BitServiceOrServiceClassUuids = 0x07,

    /// <summary> Shortened Local Name </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.2 </remarks>
    ShortenedLocalName = 0x08,

    /// <summary> Complete Local Name </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.2 </remarks>
    CompleteLocalName = 0x09,

    /// <summary> Tx Power Level </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.5 </remarks>
    TxPowerLevel = 0x0A,

    /// <summary> Class of Device </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    ClassOfDevice = 0x0D,

    /// <summary> Simple Pairing Hash C-192 </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    SimplePairingHashC192 = 0x0E,

    /// <summary> Simple Pairing Randomizer R-192 </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    SimplePairingRandomizerR192 = 0x0F,

    /// <summary> Device ID </summary>
    /// <remarks> Device ID Profile (when used in EIR data) </remarks>
    DeviceId = 0x10,

    /// <summary> Security Manager TK Value </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.8 (when used in OOB data blocks) </remarks>
    SecurityManagerTkValue = 0x10,

    /// <summary> Security Manager Out of Band Flags </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.7 </remarks>
    SecurityManagerOutOfBandFlags = 0x11,

    /// <summary> Peripheral Connection Interval Range </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.9 </remarks>
    PeripheralConnectionIntervalRange = 0x12,

    /// <summary> List of 16-bit Service Solicitation UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.10 </remarks>
    ListOf16BitServiceSolicitationUuids = 0x14,

    /// <summary> List of 128-bit Service Solicitation UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.10 </remarks>
    ListOf128BitServiceSolicitationUuids = 0x15,

    /// <summary> Service Data - 16-bit UUID </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.11 </remarks>
    ServiceData16BitUuid = 0x16,

    /// <summary> Public Target Address </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.13 </remarks>
    PublicTargetAddress = 0x17,

    /// <summary> Random Target Address </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.14 </remarks>
    RandomTargetAddress = 0x18,

    /// <summary> Appearance </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.12 </remarks>
    Appearance = 0x19,

    /// <summary> Advertising Interval </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.15 </remarks>
    AdvertisingInterval = 0x1A,

    /// <summary> LE Bluetooth Device Address </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.16 </remarks>
    LeBluetoothDeviceAddress = 0x1B,

    /// <summary> LE Role </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.17 </remarks>
    LeRole = 0x1C,

    /// <summary> Simple Pairing Hash C-256 </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    SimplePairingHashC256 = 0x1D,

    /// <summary> Simple Pairing Randomizer R-256 </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    SimplePairingRandomizerR256 = 0x1E,

    /// <summary> List of 32-bit Service Solicitation UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.10 </remarks>
    ListOf32BitServiceSolicitationUuids = 0x1F,

    /// <summary> Service Data - 32-bit UUID </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.11 </remarks>
    ServiceData32BitUuid = 0x20,

    /// <summary> Service Data - 128-bit UUID </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.11 </remarks>
    ServiceData128BitUuid = 0x21,

    /// <summary> LE Secure Connections Confirmation Value </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    LeSecureConnectionsConfirmationValue = 0x22,

    /// <summary> LE Secure Connections Random Value </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.6 </remarks>
    LeSecureConnectionsRandomValue = 0x23,

    /// <summary> URI </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.18 </remarks>
    Uri = 0x24,

    /// <summary> Indoor Positioning </summary>
    /// <remarks> Indoor Positioning Service </remarks>
    IndoorPositioning = 0x25,

    /// <summary> Transport Discovery Data </summary>
    /// <remarks> Transport Discovery Service </remarks>
    TransportDiscoveryData = 0x26,

    /// <summary> LE Supported Features </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.19 </remarks>
    LeSupportedFeatures = 0x27,

    /// <summary> Channel Map Update Indication </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.20 </remarks>
    ChannelMapUpdateIndication = 0x28,

    /// <summary> PB-ADV </summary>
    /// <remarks> Mesh Profile Specification, Section 5.2.1 </remarks>
    Pbadv = 0x29,

    /// <summary> Mesh Message </summary>
    /// <remarks> Mesh Profile Specification, Section 3.3.1 </remarks>
    MeshMessage = 0x2A,

    /// <summary> Mesh Beacon </summary>
    /// <remarks> Mesh Profile Specification, Section 3.9 </remarks>
    MeshBeacon = 0x2B,

    /// <summary> BIGInfo </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.21 </remarks>
    BigInfo = 0x2C,

    /// <summary> Broadcast_Code </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.22 </remarks>
    BroadcastCode = 0x2D,

    /// <summary> Resolvable Set Identifier </summary>
    /// <remarks> Coordinated Set Identification Profile v1.0 or later </remarks>
    ResolvableSetIdentifier = 0x2E,

    /// <summary> Advertising Interval - long </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.15 </remarks>
    AdvertisingIntervalLong = 0x2F,

    /// <summary> Broadcast_Name </summary>
    /// <remarks> Public Broadcast Profile v1.0 or later </remarks>
    BroadcastName = 0x30,

    /// <summary> Encrypted Advertising Data </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.23 </remarks>
    EncryptedAdvertisingData = 0x31,

    /// <summary> Periodic Advertising Response Timing Information </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.24 </remarks>
    PeriodicAdvertisingResponseTimingInformation = 0x32,

    /// <summary> Electronic Shelf Label </summary>
    /// <remarks> ESL Profile </remarks>
    ElectronicShelfLabel = 0x34,

    /// <summary> 3D Information Data </summary>
    /// <remarks> 3D Synchronization Profile </remarks>
    ThreeDInformationData = 0x3D,

    /// <summary> Manufacturer Specific Data </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.4 </remarks>
    ManufacturerSpecificData = 0xFF,
}
