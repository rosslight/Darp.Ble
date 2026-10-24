namespace Darp.Ble.Connections;

/// <summary> Why a peripheral could not be connected </summary>
public enum BleConnectErrorKind
{
    /// <summary> The peripheral did not answer, e.g. it is out of range or does not advertise connectable </summary>
    Unreachable,

    /// <summary> Connecting took longer than allowed </summary>
    TimedOut,

    /// <summary> The peripheral or the platform refused the connection </summary>
    Rejected,

    /// <summary> The adapter is busy with another connection attempt </summary>
    Busy,
}
