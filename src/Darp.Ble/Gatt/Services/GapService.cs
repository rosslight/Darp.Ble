using System.Text;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Gatt.Services;

#pragma warning disable MA0048 // File name must match type name

/// <summary> The service contract for a GAP service </summary>
/// <seealso hcref="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-access-profile.html#UUID-37d5043e-0d5b-e174-a1fa-91006b88a3db"/>
public static class GapServiceContract
{
    /// <summary> The uuid of the service </summary>
    public static ServiceDeclaration GapService => new(0x1800);

    /// <summary> The Device Name characteristic </summary>
    public static TypedCharacteristicDeclaration<string, Read> DeviceNameCharacteristic { get; } =
        CharacteristicDeclaration.Create<Read>(0x2A00, Encoding.UTF8);

    /// <summary> The Appearance characteristic </summary>
    public static TypedCharacteristicDeclaration<AppearanceValues, Read> AppearanceCharacteristic { get; } =
        CharacteristicDeclaration.Create<AppearanceValues, Read>(0x2A01);

    /// <summary> Add the GAP service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static GattClientGapService AddGapService(this IBlePeripheral peripheral)
    {
        ArgumentNullException.ThrowIfNull(peripheral);

        // Check if service was added already
        if (peripheral.TryGetService(GapService, out _))
            throw new Exception("The GapService may be added only once");

        // Add the client service
        IGattClientService service = peripheral.AddService(GapService);

        // Add the characteristics
        GattTypedClientCharacteristic<string, Read> deviceNameCharacteristic = service.AddCharacteristic(
            DeviceNameCharacteristic,
            onRead: (_, _) => peripheral.Device.Name ?? "n/a"
        );
        GattTypedClientCharacteristic<AppearanceValues, Read> appearanceCharacteristic = service.AddCharacteristic(
            AppearanceCharacteristic,
            onRead: (_, _) => peripheral.Device.Appearance
        );

        return new GattClientGapService(service)
        {
            DeviceName = deviceNameCharacteristic,
            Appearance = appearanceCharacteristic,
        };
    }

    /// <summary> Get the GAP service that was added to the peripheral </summary>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static GattClientGapService GetGapService(this IBlePeripheral peripheral)
    {
        ArgumentNullException.ThrowIfNull(peripheral);

        // Add the client service
        IGattClientService service = peripheral.GetService(GapService);

        return new GattClientGapService(service)
        {
            DeviceName = service.GetCharacteristic(DeviceNameCharacteristic),
            Appearance = service.GetCharacteristic(AppearanceCharacteristic),
        };
    }

    /// <summary> Discover the GAP server service </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerGapService> DiscoverGapServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer
            .DiscoverServiceAsync(GapService, cancellationToken)
            .ConfigureAwait(false);

        // Discover the characteristics
        await service.DiscoverCharacteristicsAsync(cancellationToken).ConfigureAwait(false);

        TypedGattServerCharacteristic<string, Read> deviceNameCharacteristic = service.GetCharacteristic(
            DeviceNameCharacteristic
        );
        TypedGattServerCharacteristic<AppearanceValues, Read> appearanceCharacteristic = service.GetCharacteristic(
            AppearanceCharacteristic
        );

        return new GattServerGapService(service)
        {
            DeviceName = deviceNameCharacteristic,
            Appearance = appearanceCharacteristic,
        };
    }
}

/// <summary> The GAP Service wrapper representing the gatt client </summary>
public sealed class GattClientGapService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> Device Name characteristic </summary>
    public required GattTypedClientCharacteristic<string, Read> DeviceName { get; init; }

    /// <summary> Appearance characteristic </summary>
    public required GattTypedClientCharacteristic<AppearanceValues, Read> Appearance { get; init; }
}

/// <summary> The GAP Service wrapper representing the gatt server </summary>
public sealed class GattServerGapService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> Device Name characteristic </summary>
    public required TypedGattServerCharacteristic<string, Read> DeviceName { get; init; }

    /// <summary> Appearance characteristic </summary>
    public required TypedGattServerCharacteristic<AppearanceValues, Read> Appearance { get; init; }
}
