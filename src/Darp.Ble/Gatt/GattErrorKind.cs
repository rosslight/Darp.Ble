namespace Darp.Ble.Gatt;

/// <summary> Why a GATT operation failed </summary>
public enum GattErrorKind
{
    /// <summary> The device answered with an ATT error; see <see cref="GattException.AttStatus"/> </summary>
    AttError,

    /// <summary> The device does not have what was asked for, e.g. a service </summary>
    NotFound,

    /// <summary> The characteristic does not support the operation, or not the properties a declaration asks for </summary>
    PropertyMismatch,
}
