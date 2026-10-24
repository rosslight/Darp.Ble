#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Stays connected to a heart rate sensor: retries a failed connect while the failure may pass, and reconnects when the
// link is lost. Stops after ten seconds.
// The mock backend adds a simulated sensor that leaves range after two seconds and comes back three seconds later,
// so this runs without Bluetooth hardware.
// Usage: dotnet run examples/connect-retry.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock;

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
    {
        // Mock only, so it goes with AddMock when the example runs on hardware
        var range = new MockRange();
        mock.AddPeripheral(
            async (sensor, background) =>
            {
                LocalHeartRateService heartRate = await sensor.Peripheral.AddHeartRateServiceAsync();
                // autoRestart advertises again once the central is gone, so it can reconnect
                await sensor.Broadcaster.StartAdvertisingAsync(
                    AdvertisingData.Empty.WithCompleteListOfServiceUuids([heartRate.Service.Uuid]),
                    interval: TimeSpan.FromMilliseconds(100),
                    autoRestart: true
                );
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
                background.Run(async cancellationToken =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), background.TimeProvider, cancellationToken);
                    Console.WriteLine("Sensor leaves range");
                    range.Leave();
                    await Task.Delay(TimeSpan.FromSeconds(3), background.TimeProvider, cancellationToken);
                    Console.WriteLine("Sensor is back in range");
                    range.Enter();
                });
            },
            range: range
        );
    })
);

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
if (adapters.Count == 0)
{
    Console.Error.WriteLine("No adapter found");
    return 1;
}
await using BleAdapter adapter = await adapters[0].OpenAsync();

using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    IGapAdvertisement advertisement = await adapter
        .Observer.ScanAsync(stop.Token)
        .WhereConnectable()
        .WhereService(HeartRateServiceContract.HeartRateService.Uuid)
        .FirstAsync(stop.Token);
    while (true)
    {
        await using ConnectedPeripheral connection = await ConnectWithRetryAsync(
            adapter.Central,
            advertisement.Address,
            stop.Token
        );
        Console.WriteLine($"Connected to {connection.Address}");
        try
        {
            RemoteHeartRateService heartRate = await connection.DiscoverHeartRateServiceAsync(stop.Token);
            using NotificationStream<HeartRateMeasurement> stream = heartRate.HeartRateMeasurement.Subscribe();
            await heartRate.HeartRateMeasurement.EnableNotificationsAsync(stop.Token);
            await foreach (HeartRateMeasurement measurement in stream.WithCancellation(stop.Token))
                Console.WriteLine($"Heart rate: {measurement.Value} bpm");
        }
        catch (BleDisconnectedException e) when (e.IsTransient)
        {
            // The link was lost, not ended by this app; an app that only holds the connection awaits Disconnected instead
            Console.WriteLine($"Link lost: {e.Info.Reason}; reconnecting");
        }
    }
}
catch (OperationCanceledException) when (stop.IsCancellationRequested)
{
    Console.WriteLine("Stopped");
}
return 0;

// Retries while the failure may pass, e.g. while the peripheral is out of range; throws any other failure, and the last one
static async Task<ConnectedPeripheral> ConnectWithRetryAsync(
    BleCentral central,
    BleAddress address,
    CancellationToken cancellationToken
)
{
    const int maxAttempts = 10;
    for (var attempt = 1; ; attempt++)
    {
        // HciHost and WinRT keep trying until the token is cancelled, so each attempt gets its own time limit
        using var attemptTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptTimeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            return await central.ConnectAsync(address, cancellationToken: attemptTimeout.Token);
        }
        catch (BleException e) when (e.IsTransient && attempt < maxAttempts)
        {
            Console.WriteLine($"Attempt {attempt} failed: {e.Message}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
        {
            Console.WriteLine($"Attempt {attempt} timed out");
        }
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }
}
