using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Gatt.Services;

using ServiceChangeRange = (ushort StartHandle, ushort EndHandle);

#pragma warning disable MA0048 // File name must match type name

/// <summary> The service contract for a GAP service </summary>
/// <seealso hcref="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-access-profile.html#UUID-37d5043e-0d5b-e174-a1fa-91006b88a3db"/>
public static class GattServiceContract
{
    /// <summary> The uuid of the service </summary>
    public static ServiceDeclaration GattService => new(0x1801);

    /// <summary> The Device Name characteristic </summary>
    public static TypedCharacteristicDeclaration<ServiceChangeRange, Indicate> ServiceChangedCharacteristic { get; } =
        CharacteristicDeclaration.Create<ServiceChangeRange, Indicate>(0x2A05);

    /// <summary> The Database Hash characteristic contains the result of a hash function applied to the service definitions in the GATT database </summary>
    public static TypedCharacteristicDeclaration<UInt128, Read> DatabaseHashCharacteristic { get; } =
        CharacteristicDeclaration.Create<UInt128, Read>(0x2B2A);

    /// <summary> Add the GAP service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static GattClientGattService AddGattService(this IBlePeripheral peripheral)
    {
        ArgumentNullException.ThrowIfNull(peripheral);

        // Check if service was added already
        if (peripheral.TryGetService(GattService, out _))
            throw new Exception("The GattService may be added only once");

        // Add the client service
        IGattClientService service = peripheral.AddService(GattService);

        // Add the characteristics
        GattTypedClientCharacteristic<ServiceChangeRange, Indicate> serviceChangedChar = service.AddCharacteristic(
            ServiceChangedCharacteristic
        );
        GattTypedClientCharacteristic<UInt128, Read> databaseHashChar = service.AddCharacteristic(
            DatabaseHashCharacteristic,
            onRead: _ => peripheral.GattDatabase.CreateHash()
        );

        return new GattClientGattService(service)
        {
            ServiceChanged = serviceChangedChar,
            DatabaseHash = databaseHashChar,
        };
    }

    /// <summary> Get the GAP service that was added to the peripheral </summary>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static GattClientGattService GetGattService(this IBlePeripheral peripheral)
    {
        ArgumentNullException.ThrowIfNull(peripheral);

        // Add the client service
        IGattClientService service = peripheral.GetService(GattService);

        return new GattClientGattService(service)
        {
            ServiceChanged = service.GetCharacteristic(ServiceChangedCharacteristic),
            DatabaseHash = service.GetCharacteristic(DatabaseHashCharacteristic),
        };
    }

    /// <summary> Discover the GAP server service </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerGattService> DiscoverGattServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer
            .DiscoverServiceAsync(GattService, cancellationToken)
            .ConfigureAwait(false);

        // Discover the characteristics
        await service.DiscoverCharacteristicsAsync(cancellationToken).ConfigureAwait(false);

        TypedGattServerCharacteristic<ServiceChangeRange, Indicate> deviceNameCharacteristic =
            service.GetCharacteristic(ServiceChangedCharacteristic);
        TypedGattServerCharacteristic<UInt128, Read> databaseHashCharacteristic = service.GetCharacteristic(
            DatabaseHashCharacteristic
        );

        return new GattServerGattService(service)
        {
            ServiceChanged = deviceNameCharacteristic,
            DatabaseHash = databaseHashCharacteristic,
        };
    }
}

/// <summary> The Gatt Service wrapper representing the gatt client </summary>
public sealed class GattClientGattService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> Service changed characteristic </summary>
    public required GattTypedClientCharacteristic<ServiceChangeRange, Indicate> ServiceChanged { get; init; }

    /// <summary> Database hash characteristic </summary>
    public required GattTypedClientCharacteristic<UInt128, Read> DatabaseHash { get; init; }
}

/// <summary> The Gatt Service wrapper representing the gatt server </summary>
public sealed class GattServerGattService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> Service changed characteristic </summary>
    public required TypedGattServerCharacteristic<ServiceChangeRange, Indicate> ServiceChanged { get; init; }

    /// <summary> Database hash characteristic </summary>
    public required TypedGattServerCharacteristic<UInt128, Read> DatabaseHash { get; init; }
}
