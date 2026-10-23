namespace Darp.Ble.Gatt.Services;

/// <summary> The battery service of a connected peripheral </summary>
/// <param name="BatteryLevel"> The battery level in percent </param>
/// <param name="NotifyingBatteryLevel"> The same characteristic for notifications, when the peripheral notifies level changes </param>
public sealed record RemoteBatteryService(
    TypedRemoteCharacteristic<byte, Properties.Read> BatteryLevel,
    TypedRemoteCharacteristic<byte, Properties.Notify>? NotifyingBatteryLevel
);
