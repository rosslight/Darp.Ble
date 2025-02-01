using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Gatt.Services;

/// <summary> The service contract for a battery service </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/bas-1-1/"/>
public static class BatteryServiceContract
{
    /// <summary> The uuid of the service </summary>
    /// <value> 0x180F </value>
    public static ServiceDeclaration BatteryService => new (0x180F);
    /// <summary> The battery level in percent </summary>
    /// <value> 0x2A19 </value>
    public static TypedCharacteristicDeclaration<byte, Read, Notify> BatteryLevelCharacteristic { get; } =
        CharacteristicDeclaration.Create<byte, Read, Notify>(0x2A19);

    /// <summary> Add a new DeviceInformationService to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="initialBatteryLevel"> The initial battery level </param>
    /// <param name="batteryLevelDescription"> The description of the battery level characteristic; No descriptor will be added if <c>null</c> </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which contains the service on completion </returns>
    public static async Task<GattClientBatteryService> AddBatteryService(
        this IBlePeripheral peripheral,
        byte initialBatteryLevel = 50,
        string? batteryLevelDescription = "main",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(BatteryService, cancellationToken).ConfigureAwait(false);

        GattTypedClientCharacteristic<byte, Read, Notify> batteryLevelCharacteristic = await service.AddCharacteristicAsync(
                BatteryLevelCharacteristic,
                staticValue: initialBatteryLevel,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (batteryLevelDescription is not null)
        {
            // await batteryLevelCharacteristic.AddUserDescriptionAsync(batteryLevelDescription, cancellationToken);
        }

        return new GattClientBatteryService(service)
        {
            BatteryLevel = batteryLevelCharacteristic,
        };
    }

    /// <summary> Discover the battery service </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerBatteryService> DiscoverBatteryServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer.DiscoverServiceAsync(BatteryService, cancellationToken).ConfigureAwait(false);

        // Discover the characteristics
        await service.DiscoverCharacteristicsAsync(cancellationToken).ConfigureAwait(false);
        TypedGattServerCharacteristic<byte, Read, Notify> batteryLevelCharacteristic = service
            .GetCharacteristic(BatteryLevelCharacteristic);

        return new GattServerBatteryService(service) { BatteryLevel = batteryLevelCharacteristic };
    }
}

/// <summary> The BatteryService wrapper representing the gatt client </summary>
public sealed class GattClientBatteryService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> The battery level characteristic </summary>
    public required GattTypedClientCharacteristic<byte, Read, Notify> BatteryLevel { get; init; }
}

/// <summary> The BatteryService wrapper representing the gatt server </summary>
public sealed class GattServerBatteryService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> The battery level characteristic </summary>
    public required TypedGattServerCharacteristic<byte, Read, Notify> BatteryLevel { get; init; }
}
