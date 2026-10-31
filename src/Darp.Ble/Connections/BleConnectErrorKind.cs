namespace Darp.Ble.Connections;

/// <summary> Why a peripheral could not be connected </summary>
public enum BleConnectErrorKind
{
    /// <summary> The link failed to come up or went down before the connection was set up, or the backend knows no connectable device at the address </summary>
    /// <remarks> A peripheral out of range does not end the attempt on HciHost and WinRT; they keep trying until the token is cancelled </remarks>
    Unreachable,

    /// <summary> The adapter did not answer the request to connect </summary>
    TimedOut,

    /// <summary> The peripheral or the platform refused the connection </summary>
    Rejected,

    /// <summary> The adapter is busy with another connection attempt </summary>
    Busy,
}
