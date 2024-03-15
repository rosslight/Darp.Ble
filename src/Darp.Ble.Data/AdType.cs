namespace Darp.Ble.Data;

/// <summary> The Ad types </summary>
public enum AdType : byte
{
    /// <summary> Flags </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.3 </remarks>
    Flags = 0x01,
    /// <summary> Incomplete List of 16-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteService16BitUuids = 0x02,
    /// <summary> Complete List of 16-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteService16BitUuids = 0x03,
    /// <summary> Incomplete List of 32-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteService32BitUuids = 0x04,
    /// <summary> Complete List of 32-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteService32BitUuids = 0x05,
    /// <summary> Incomplete List of 128-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    IncompleteService128BitUuids = 0x06,
    /// <summary> Complete List of 128-bit Service Class UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.1 </remarks>
    CompleteService128BitUuids = 0x07,
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
    /// <remarks> Device ID Profile </remarks>
    DeviceIdProfile = 0x10,
    /// <summary> Security Manager TK Value </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.8 </remarks>
    SecurityManagerTkValues = 0x10,
    /// <summary> Security Manager Out of Band Flags </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.7 </remarks>
    SecurityManagerOutOfBandFlags = 0x11,
    /// <summary> Peripheral Connection Interval Range </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.9 </remarks>
    SlaveConnectionIntervalRange = 0x12,
    /// <summary> List of 16-bit Service Solicitation UUIDs </summary>
    /// <remarks> Core Specification Supplement, Part A, Section 1.10 </remarks>
    ServiceSolicitation16BitUuids = 0x14,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ServiceSolicitation32BitUuids = 0x1F,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ServiceSolicitation128BitUuids = 0x15,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ServiceData16BitUuids = 0x16,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ServiceData32BitUuids = 0x20,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ServiceData128BitUuids = 0x21,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    PublicTargetAddress = 0x17,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    RandomTargetAddress = 0x18,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    Appearance = 0x19,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    AdvertisingInterval = 0x1A,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    LeBluetoothDeviceAddress = 0x1B,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    LeRole = 0x1C,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    SimplePairingHashC256 = 0x1D,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    SimplePairingRandomizerR256 = 0x1E,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ThreeDimensionInformationData = 0x3D,
    /// <summary>  </summary>
    /// <remarks>  </remarks>
    ManufacturerSpecificData = 0xFF,
}