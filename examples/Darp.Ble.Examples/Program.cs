// See https://aka.ms/new-console-template for more information

using Darp.Ble;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.WinRT;

BleManager manager = new BleManagerBuilder()
    .With<WinBleFactory>()
    .CreateManager();

IBleDevice x = manager.EnumerateDevices().First();
await x.InitializeAsync();
IGattClientService service = await x.Peripheral.AddServiceAsync(0x1234);
IGattClientCharacteristic<Properties.Write> characteristic = await service.AddCharacteristicAsync<Properties.Write>(0x1235);
characteristic.OnWrite((peer, bytes) =>
{
    int i = 0;
    return GattProtocolStatus.Success;
});

x.Broadcaster.Advertise(AdvertisingData.From([
        (AdTypes.CompleteListOf16BitServiceClassUuids, [0x34, 0x12]),
        (AdTypes.ManufacturerSpecificData, [0x04, 0x00, 0x01, 0x02, 0x03, 0x04]),
    ]),
    TimeSpan.FromMilliseconds(100),  new AdvertisingParameters
{
    Type = BleEventType.Connectable,
});

await Task.Delay(200000);