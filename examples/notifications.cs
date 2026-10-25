#:project ../src/Darp.Ble/Darp.Ble.csproj
#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Connects to a heart rate sensor and prints five measurements it notifies.
// The mock backend adds a simulated sensor that notifies every subscribed central twice a second,
// so this runs without Bluetooth hardware.
// Usage: dotnet run examples/notifications.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock;

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            async (adapter, background) =>
            {
                LocalHeartRateService heartRate = await adapter.Peripheral.AddHeartRateServiceAsync();
                await adapter.Broadcaster.StartAdvertisingAsync(
                    AdvertisingData.Empty.WithCompleteListOfServiceUuids([heartRate.Service.Uuid]),
                    interval: TimeSpan.FromMilliseconds(100)
                );
                // The simulated sensor runs until the mock adapter closes
                background.Run(async cancellationToken =>
                {
                    ushort bpm = 60;
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500), background.TimeProvider);
                    while (await timer.WaitForNextTickAsync(cancellationToken))
                        await heartRate.HeartRateMeasurement.NotifyAllAsync(
                            new HeartRateMeasurement(++bpm),
                            cancellationToken
                        );
                });
            }
        )
    )
);

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
if (adapters.Count == 0)
{
    Console.Error.WriteLine("No adapter found");
    return 1;
}
await using BleAdapter adapter = await adapters[0].OpenAsync();

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
IGapAdvertisement sensor = await adapter
    .Observer.ScanAsync(timeout.Token)
    .WhereConnectable()
    .WhereService(HeartRateServiceContract.HeartRateService.Uuid)
    .FirstAsync(timeout.Token);
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
    sensor.Address,
    cancellationToken: timeout.Token
);

// The same contract the simulated sensor hosts: the client decodes each measurement, whatever its flags say
RemoteHeartRateService heartRate = await connection.DiscoverHeartRateServiceAsync(timeout.Token);

// Subscribing does no I/O; the stream gets every notification from here on, even the first one after enabling
using NotificationStream<HeartRateMeasurement> stream = heartRate.HeartRateMeasurement.Subscribe();
await heartRate.HeartRateMeasurement.EnableNotificationsAsync(timeout.Token);
await foreach (HeartRateMeasurement measurement in stream.Take(5).WithCancellation(timeout.Token))
    Console.WriteLine($"Heart rate: {measurement.Value} bpm");
return 0;
