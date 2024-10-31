namespace Darp.Ble.Data;

/// <summary> Advertisement paramters to configure advertisement </summary>
public sealed record AdvertisingParameters
{
    /// <summary> The <see cref="BleEventType"/> to be used when sending the advertisement </summary>
    public required BleEventType Type { get; init; }
}