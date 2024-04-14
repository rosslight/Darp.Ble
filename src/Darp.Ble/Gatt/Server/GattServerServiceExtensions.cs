namespace Darp.Ble.Gatt.Server;

public static class GattServerServiceExtensions
{
    public static async Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this GattServerService service, Characteristic<TProp1> characteristic)
    {
        GattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(characteristic.Uuid);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }
}