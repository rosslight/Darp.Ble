using Darp.Ble.Data;

namespace Darp.Ble.Connections;

/// <summary> The parameters used when configuring a ble scan </summary>
public sealed record BleConnectionParameters
{
    /// <summary> The connection interval </summary>
    public ConnectionTiming ConnectionInterval { get; init; } = ConnectionTiming.Ms100;
}
