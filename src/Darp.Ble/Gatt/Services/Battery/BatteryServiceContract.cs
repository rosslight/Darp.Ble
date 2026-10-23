using Darp.Ble.Connections;

namespace Darp.Ble.Gatt.Services;

/// <summary> The Battery service: the battery level in percent </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/bas-1-1/"/>
public static class BatteryServiceContract
{
    /// <summary> The declaration of the service, 0x180F </summary>
    public static ServiceDeclaration BatteryService { get; } = new(0x180F);

    /// <summary> The battery level in percent, 0x2A19, as hosted; the service makes Notify optional, so discovery requires only Read </summary>
    public static TypedCharacteristicDeclaration<
        byte,
        Properties.Read,
        Properties.Notify
    > BatteryLevelCharacteristic { get; } =
        CharacteristicDeclaration.Create<byte, Properties.Read, Properties.Notify>(0x2A19);

    /// <summary> What discovery requires of the battery level </summary>
    private static readonly CharacteristicDeclaration<Properties.Read> ReadableBatteryLevel = new(0x2A19);

    /// <summary> Adds a battery service </summary>
    /// <param name="peripheral"> The adapter's peripheral role </param>
    /// <param name="initialBatteryLevel"> The level centrals read until it changes </param>
    /// <param name="batteryLevelDescription"> The user description of the level; none when null </param>
    /// <param name="cancellationToken"> Cancels adding </param>
    /// <returns> The service; <see cref="LocalBatteryService.SetBatteryLevelAsync"/> changes the level </returns>
    public static async Task<LocalBatteryService> AddBatteryServiceAsync(
        this BlePeripheral peripheral,
        byte initialBatteryLevel = 50,
        string? batteryLevelDescription = "main",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        // The service holds the level, but exists only once it was added; until the app has it, the level is the initial one
        LocalBatteryService? battery = null;
        var builder = new LocalGattServiceBuilder(BatteryService);
        TypedLocalCharacteristic<byte, Properties.Read, Properties.Notify> characteristic = builder.AddCharacteristic(
            BatteryLevelCharacteristic,
            onRead: (_, _) => ValueTask.FromResult(battery?.BatteryLevel ?? initialBatteryLevel)
        );
        if (batteryLevelDescription is not null)
            characteristic.Characteristic.AddUserDescription(batteryLevelDescription);
        LocalGattService service = await peripheral.AddServiceAsync(builder, cancellationToken).ConfigureAwait(false);
        battery = new LocalBatteryService(service, characteristic, initialBatteryLevel);
        return battery;
    }

    /// <summary> Discovers the battery service of a connected peripheral </summary>
    /// <param name="connection"> The connection to the peripheral </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service's characteristics; <see cref="RemoteBatteryService.NotifyingBatteryLevel"/> is null when the level does not notify </returns>
    public static async Task<RemoteBatteryService> DiscoverBatteryServiceAsync(
        this ConnectedPeripheral connection,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        RemoteGattService service = await connection
            .DiscoverServiceAsync(BatteryService, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service
            .DiscoverCharacteristicsAsync(cancellationToken)
            .ConfigureAwait(false);
        RemoteGattCharacteristic level = RemoteGattServiceExtensions.Require(
            service,
            characteristics,
            ReadableBatteryLevel
        );
        return new RemoteBatteryService(
            new TypedRemoteCharacteristic<byte, Properties.Read>(level, BatteryLevelCharacteristic),
            level.Properties.HasFlag(GattProperty.Notify)
                ? new TypedRemoteCharacteristic<byte, Properties.Notify>(level, BatteryLevelCharacteristic)
                : null
        );
    }
}
