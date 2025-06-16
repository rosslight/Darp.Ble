namespace Darp.Ble.Data;

/// <summary> The parameters used when configuring a ble scan </summary>
public sealed record BleConnectionParameters
{
    /// <summary> The connection interval </summary>
    public ConnectionTiming ConnectionInterval { get; init; } = ConnectionTiming.Ms100;
}
