namespace Darp.Ble.Data;

/// <summary> The status of a Gatt request. </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/CSS_v11/out/en/supplement-to-the-bluetooth-core-specification/common-profile-and-service-error-codes.html">Part B. Common Profile and Service Error Codes</seealso>
public enum GattProtocolStatus : byte
{
    /// <summary> Success </summary>
    Success = 0x00,
    // 0xE0 – 0xFB = Reserved for Future Use
    /// <summary> Write Request Rejected </summary>
    WriteRequestRejected = 0xFC,
    /// <summary> Client Characteristic Configuration Descriptor Improperly Configured </summary>
    ClientCharacteristicConfigurationDescriptorImproperlyConfigured = 0xFD,
    /// <summary> Procedure Already in Progress </summary>
    ProcedureAlreadyInProgress = 0xFE,
    /// <summary> Out of Range </summary>
    OutOfRange = 0xFF,
}