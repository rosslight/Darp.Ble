namespace Darp.Ble.Gatt;

/// <summary> The status of a GATT request: success, an ATT error (Core spec Vol 3 Part F 3.4.1.1) or a common profile error </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/CSS_v11/out/en/supplement-to-the-bluetooth-core-specification/common-profile-and-service-error-codes.html">Part B. Common Profile and Service Error Codes</seealso>
public enum GattProtocolStatus : byte
{
    /// <summary> Success </summary>
    Success = 0x00,

    /// <summary> ATT error: The attribute handle is not valid on this server </summary>
    InvalidHandle = 0x01,

    /// <summary> ATT error: The attribute cannot be read </summary>
    ReadNotPermitted = 0x02,

    /// <summary> ATT error: The attribute cannot be written </summary>
    WriteNotPermitted = 0x03,

    /// <summary> ATT error: The attribute PDU was invalid </summary>
    InvalidPdu = 0x04,

    /// <summary> ATT error: The attribute requires authentication before it can be read or written </summary>
    InsufficientAuthentication = 0x05,

    /// <summary> ATT error: The server does not support the request </summary>
    RequestNotSupported = 0x06,

    /// <summary> ATT error: The offset is past the end of the attribute </summary>
    InvalidOffset = 0x07,

    /// <summary> ATT error: The attribute requires authorization before it can be read or written </summary>
    InsufficientAuthorization = 0x08,

    /// <summary> ATT error: Too many prepared writes have been queued </summary>
    PrepareQueueFull = 0x09,

    /// <summary> ATT error: No attribute was found within the given handle range </summary>
    AttributeNotFound = 0x0A,

    /// <summary> ATT error: The attribute cannot be read with a blob request </summary>
    AttributeNotLong = 0x0B,

    /// <summary> ATT error: The encryption key size of the link is too short </summary>
    EncryptionKeySizeTooShort = 0x0C,

    /// <summary> ATT error: The value length is invalid for the operation </summary>
    InvalidAttributeValueLength = 0x0D,

    /// <summary> ATT error: The request hit an error that was unlikely and could not be completed </summary>
    UnlikelyError = 0x0E,

    /// <summary> ATT error: The attribute requires encryption before it can be read or written </summary>
    InsufficientEncryption = 0x0F,

    /// <summary> ATT error: The attribute type is not a supported grouping attribute </summary>
    UnsupportedGroupType = 0x10,

    /// <summary> ATT error: The server lacked the resources to complete the request </summary>
    InsufficientResources = 0x11,

    /// <summary> ATT error: The server requests the client to rediscover the database </summary>
    DatabaseOutOfSync = 0x12,

    /// <summary> ATT error: The attribute parameter value was not allowed </summary>
    ValueNotAllowed = 0x13,

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
