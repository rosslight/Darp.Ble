#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Connects to a heart rate sensor and prints five measurements it notifies.
// The mock backend adds a simulated sensor that notifies every connected central twice a second,
// so this runs without Bluetooth hardware.
// Usage: dotnet run examples/notifications.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;

BleUuid heartRateService = BleUuid.FromUInt16(0x180D);
BleUuid measurementUuid = BleUuid.FromUInt16(0x2A37);

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            async (simulated, background) =>
            {
                var service = new LocalGattServiceBuilder(heartRateService);
                LocalGattCharacteristic measurement = service.AddCharacteristic(measurementUuid, GattProperty.Notify);
                await simulated.Peripheral.AddServiceAsync(service);
                await simulated.Broadcaster.StartAdvertisingAsync(
                    AdvertisingData.Empty.WithCompleteListOfServiceUuids([heartRateService]),
                    interval: TimeSpan.FromMilliseconds(100)
                );
                // The simulated sensor runs until the mock adapter closes: flags 0 (8-bit value), then the beats per minute
                background.Run(async cancellationToken =>
                {
                    byte bpm = 60;
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500), background.TimeProvider);
                    while (await timer.WaitForNextTickAsync(cancellationToken))
                    {
                        bpm++;
                        foreach (ConnectedCentral central in simulated.Peripheral.ConnectedCentrals)
                            await measurement.NotifyAsync(central, new byte[] { 0, bpm }, cancellationToken);
                    }
                });
            }
        )
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
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
    sensor.Address,
    cancellationToken: timeout.Token
);
RemoteGattService service = await connection.DiscoverServiceAsync(heartRateService, timeout.Token);
RemoteCharacteristic<Properties.Notify> measurement = await service.DiscoverCharacteristicAsync(
    new CharacteristicDeclaration<Properties.Notify>(measurementUuid),
    timeout.Token
);

// Subscribing does no I/O; the stream gets every notification from here on, even the first one after enabling
using NotificationStream<byte[]> stream = measurement.Subscribe();
await measurement.EnableNotificationsAsync(timeout.Token);
await foreach (byte[] value in stream.Take(5).WithCancellation(timeout.Token))
    Console.WriteLine($"Heart rate: {value[1]} bpm");
