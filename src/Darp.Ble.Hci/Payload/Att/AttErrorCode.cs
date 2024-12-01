namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The type of the <see cref="AttErrorRsp"/> </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9f07d82d-da59-ca27-4ee2-b404bbba3f54_informaltable-idm13358908515990"/>
public enum AttErrorCode : byte
{
    /// <summary> <see cref="None"/> error code </summary>
    None,
    /// <summary> The attribute handle given was not valid on this server </summary>
    InvalidHandle = 0x01,
    /// <summary> The attribute cannot be read </summary>
    ReadNotPermittedError = 0x02,
    /// <summary> The attribute cannot be written </summary>
    WriteNotPermittedError = 0x03,
    /// <summary> The attribute PDU was invalid </summary>
    InvalidPduError = 0x04,
    /// <summary> The attribute requires authentication before it can be read or written </summary>
    InsufficientAuthenticationError = 0x05,
    /// <summary> ATT Server does not support the request received from the client </summary>
    RequestNotSupportedError = 0x06,
    /// <summary> Offset specified was past the end of the attribute </summary>
    InvalidOffsetError = 0x07,
    /// <summary> The attribute requires authorization before it can be read or written </summary>
    InsufficientAuthorizationError = 0x08,
    /// <summary> Too many prepare writes have been queued </summary>
    PrepareQueueFullError = 0x09,
    /// <summary> No attribute found within the given attribute handle range </summary>
    AttributeNotFoundError = 0x0A,
    /// <summary> The attribute cannot be read using the ATT_READ_BLOB_REQ PDU </summary>
    AttributeNotLongError = 0x0B,
    /// <summary> The Encryption Key Size used for encrypting this link is too short </summary>
    InsufficientEncryptionKeySizeError = 0x0C,
    /// <summary> The attribute value length is invalid for the operation </summary>
    InvalidAttributeLengthError = 0x0D,
    /// <summary> The attribute request that was requested has encountered an error that was unlikely, and therefore could not be completed as requested </summary>
    UnlikelyErrorError = 0x0E,
    /// <summary> The attribute requires encryption before it can be read or written </summary>
    InsufficientEncryptionError = 0x0F,
    /// <summary> The attribute type is not a supported grouping attribute as defined by a higher layer specification </summary>
    UnsupportedGroupTypeError = 0x10,
    /// <summary> Insufficient Resources to complete the request </summary>
    InsufficientResourcesError = 0x11,
    /// <summary> The server requests the client to rediscover the database </summary>
    DatabaseOutOfSync = 0x12,
    /// <summary> The attribute parameter value was not allowed </summary>
    ValueNotAllowed = 0x13,
    // Application Error: 0x80 – 0x9F
    // Common Profile and Service Error Codes: 0xE0 – 0xFF
}