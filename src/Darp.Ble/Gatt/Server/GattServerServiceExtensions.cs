using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public static class GattServerServiceExtensions
{
    public static async Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(uuid, cancellationToken);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }
    public static Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        ushort uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return service.DiscoverCharacteristicAsync<TProp1>(new BleUuid(uuid), cancellationToken);
    }

    public static async Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        Characteristic<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(characteristic.Uuid, cancellationToken);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }
}