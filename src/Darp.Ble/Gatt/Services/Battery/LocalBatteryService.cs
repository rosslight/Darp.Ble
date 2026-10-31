namespace Darp.Ble.Gatt.Services;

/// <summary> A battery service the adapter hosts </summary>
/// <param name="Service"> The hosted service, e.g. to advertise its UUID </param>
/// <param name="BatteryLevelCharacteristic"> The battery level characteristic </param>
/// <param name="BatteryLevel"> The level centrals read until <see cref="SetBatteryLevelAsync"/> changes it </param>
public sealed record LocalBatteryService(
    LocalGattService Service,
    TypedLocalCharacteristic<byte, Properties.Read, Properties.Notify> BatteryLevelCharacteristic,
    byte BatteryLevel
)
{
    // Centrals read it in a handler on another thread than the app sets it on; volatile makes them see the new level
    private volatile byte _batteryLevel = BatteryLevel;

    /// <summary> The level centrals read </summary>
    public byte BatteryLevel => _batteryLevel;

    /// <summary> Changes the level later reads return and notifies every central that turned notifications on </summary>
    /// <param name="level"> The level in percent </param>
    /// <param name="cancellationToken"> Cancels waiting to send the notifications </param>
    /// <returns> A task that completes once every central's notification was accepted or skipped </returns>
    public ValueTask SetBatteryLevelAsync(byte level, CancellationToken cancellationToken = default)
    {
        _batteryLevel = level;
        return BatteryLevelCharacteristic.NotifyAllAsync(level, cancellationToken);
    }
}
