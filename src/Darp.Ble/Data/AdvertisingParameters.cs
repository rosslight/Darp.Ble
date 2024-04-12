namespace Darp.Ble.Data;

public sealed record AdvertisingParameters
{
    public required BleEventType Type { get; init; }
}