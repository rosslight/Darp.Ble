using Darp.Ble.Data;

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

    /// <summary> Publishes a battery service </summary>
    /// <param name="gatt"> The adapter's services </param>
    /// <param name="initialBatteryLevel"> The level centrals read until it changes </param>
    /// <param name="batteryLevelDescription"> The user description of the level; none when null </param>
    /// <param name="cancellationToken"> Cancels publishing </param>
    /// <returns> The service; <see cref="LocalBatteryService.SetBatteryLevelAsync"/> changes the level </returns>
    public static async Task<LocalBatteryService> AddBatteryServiceAsync(
        this GattServer gatt,
        byte initialBatteryLevel = 50,
        string? batteryLevelDescription = "main",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gatt);
        var level = new BatteryLevel(initialBatteryLevel);
        var builder = new LocalGattServiceBuilder(BatteryService);
        TypedLocalCharacteristic<byte, Properties.Read, Properties.Notify> characteristic = builder.AddCharacteristic(
            BatteryLevelCharacteristic,
            onRead: (_, _) => ValueTask.FromResult(level.Value)
        );
        if (batteryLevelDescription is not null)
            characteristic.Characteristic.AddUserDescription(batteryLevelDescription);
        await gatt.AddServiceAsync(builder, cancellationToken).ConfigureAwait(false);
        return new LocalBatteryService(characteristic, level);
    }

    /// <summary> Discovers the battery service of a connected peripheral </summary>
    /// <param name="gatt"> The peripheral's services </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service's characteristics; <see cref="RemoteBatteryService.NotifyingBatteryLevel"/> is null when the level does not notify </returns>
    public static async Task<RemoteBatteryService> DiscoverBatteryServiceAsync(
        this GattClient gatt,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gatt);
        RemoteGattService service = await gatt.DiscoverServiceAsync(BatteryService, cancellationToken)
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
