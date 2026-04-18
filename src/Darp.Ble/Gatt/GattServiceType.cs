namespace Darp.Ble.Gatt;

/// <summary>
/// Identifies whether a GATT service is primary or secondary.
/// </summary>
public enum GattServiceType
{
    /// <summary> The service type is undefined </summary>
    Undefined,

    /// <summary> The service type is Primary </summary>
    Primary,

    /// <summary> The service type is secondary </summary>
    Secondary,
}
