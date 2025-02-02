namespace Darp.Ble.Data;

/// <summary> The result codes for initialization </summary>
public enum InitializeResult
{
    /// <summary> Initialization was successful </summary>
    Success = 0,

    /// <summary> Could not initialize because init was already running </summary>
    AlreadyInitializing,

    /// <summary> The device is not available. </summary>
    DeviceNotAvailable,

    /// <summary> The device is not enabled </summary>
    DeviceNotEnabled,

    /// <summary> The version of the device is not supported </summary>
    DeviceVersionUnsupported,

    /// <summary> The controller can not be accessed as there are permissions missing </summary>
    DeviceMissingPermissions,
}
