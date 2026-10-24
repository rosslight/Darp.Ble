namespace Darp.Ble;

/// <summary> Why an adapter could not be opened </summary>
public enum BleOpenErrorKind
{
    /// <summary> The adapter is gone, e.g. unplugged, or in use by another process </summary>
    NotAvailable,

    /// <summary> Bluetooth is turned off </summary>
    Disabled,

    /// <summary> The app lacks the permission to use Bluetooth </summary>
    MissingPermission,
}
