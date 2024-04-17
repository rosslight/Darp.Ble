namespace Darp.Ble.Hci.Payload.Att;

public enum AttErrorCode
{
    InvalidHandle = 0x01,
    ReadNotPermittedError = 0x02,
    WriteNotPermittedError = 0x03,
    InvalidPduError = 0x04,
    InsufficientAuthenticationError = 0x05,
    RequestNotSupportedError = 0x06,
    InvalidOffsetError = 0x07,
    InsufficientAuthorizationError = 0x08,
    PrepareQueueFullError = 0x09,
    AttributeNotFoundError = 0x0A,
    AttributeNotLongError = 0x0B,
    InsufficientEncryptionKeySizeError = 0x0C,
    InvalidAttributeLengthError = 0x0D,
    UnlikelyErrorError = 0x0E,
    InsufficientEncryptionError = 0x0F,
    UnsupportedGroupTypeError = 0x10,
    InsufficientResourcesError = 0x11,
}