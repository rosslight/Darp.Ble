#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Scans for a heart rate sensor, connects to it, lists its services and disconnects.
// The mock backend adds a simulated sensor that hosts the service, so this runs without Bluetooth hardware.
// Usage: dotnet run examples/connect.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;

BleUuid heartRateService = BleUuid.FromUInt16(0x180D);

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(async peripheral =>
        {
            var service = new LocalGattServiceBuilder(heartRateService);
            service.AddCharacteristic(BleUuid.FromUInt16(0x2A37), GattProperty.Notify);
            service.AddCharacteristic(BleUuid.FromUInt16(0x2A38), GattProperty.Read);
            await peripheral.Peripheral.Gatt.AddServiceAsync(service);
            await peripheral.Broadcaster.StartAdvertisingAsync(
                AdvertisingData
                    .Empty.WithCompleteLocalName("Heart rate sensor")
                    .WithCompleteListOfServiceUuids([heartRateService]),
                interval: TimeSpan.FromMilliseconds(100)
            );
        })
    )
);

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
IGapAdvertisement sensor = await adapter
    .Observer.ScanAsync(timeout.Token)
    .WhereConnectable()
    .WhereService(heartRateService)
    .FirstAsync(timeout.Token);
Console.WriteLine($"Found {sensor.Address}");

// Disposing the connection disconnects
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
    sensor.Address,
    cancellationToken: timeout.Token
);
Console.WriteLine($"Connected; a write carries up to {connection.MaxWritePayloadLength} bytes");

foreach (RemoteGattService service in await connection.Gatt.DiscoverServicesAsync(timeout.Token))
{
    Console.WriteLine($"Service {service.Uuid}");
    foreach (RemoteGattCharacteristic characteristic in await service.DiscoverCharacteristicsAsync(timeout.Token))
        Console.WriteLine($"  Characteristic {characteristic.Uuid}: {characteristic.Properties}");
}

await connection.DisconnectAsync(timeout.Token);
Console.WriteLine($"Disconnected: {(await connection.Disconnected).Reason}");
