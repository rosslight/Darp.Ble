using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Services;

/// <summary> A battery service the adapter hosts </summary>
public sealed class LocalBatteryService
{
    private readonly BatteryLevel _level;

    internal LocalBatteryService(
        TypedLocalCharacteristic<byte, Properties.Read, Properties.Notify> batteryLevel,
        BatteryLevel level
    )
    {
        BatteryLevelCharacteristic = batteryLevel;
        _level = level;
    }

    /// <summary> The battery level characteristic </summary>
    public TypedLocalCharacteristic<byte, Properties.Read, Properties.Notify> BatteryLevelCharacteristic { get; }

    /// <summary> The level centrals read </summary>
    public byte BatteryLevel => _level.Value;

    /// <summary> Changes the level later reads return and notifies every central that turned notifications on </summary>
    /// <param name="level"> The level in percent </param>
    /// <param name="cancellationToken"> Cancels waiting to send the notifications </param>
    /// <returns> A task that completes once every central's notification was accepted or skipped </returns>
    public ValueTask SetBatteryLevelAsync(byte level, CancellationToken cancellationToken = default)
    {
        _level.Value = level;
        return BatteryLevelCharacteristic.NotifyAllAsync(level, cancellationToken);
    }
}
