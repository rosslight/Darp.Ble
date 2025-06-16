namespace Darp.Ble.Gatt.Att;

/// <summary> Att error codes to return if the permission check was unsuccessful </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9f07d82d-da59-ca27-4ee2-b404bbba3f54_informaltable-idm13358908515990"/>
public enum PermissionCheckStatus
{
    /// <summary> All permissions are met </summary>
    Success,

    /// <summary> The attribute cannot be read </summary>
    ReadNotPermittedError = 0x02,

    /// <summary> The attribute cannot be written </summary>
    WriteNotPermittedError = 0x03,

    /// <summary> The attribute requires encryption before it can be read or written </summary>
    InsufficientEncryptionError = 0x0F,

    /// <summary> The attribute requires authentication before it can be read or written </summary>
    InsufficientAuthenticationError = 0x05,

    /// <summary> The attribute requires authorization before it can be read or written </summary>
    InsufficientAuthorizationError = 0x08,
}
