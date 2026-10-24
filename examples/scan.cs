#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Scans for five seconds and prints every advertisement.
// The mock backend adds a simulated heart rate sensor, so this runs without Bluetooth hardware.
// Usage: dotnet run examples/scan.cs
using Darp.Ble;
using Darp.Ble.Gap;
using Darp.Ble.Mock;

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(peripheral =>
            peripheral.Broadcaster.StartAdvertisingAsync(
                AdvertisingData.Empty.WithCompleteLocalName("Heart rate sensor"),
                interval: TimeSpan.FromMilliseconds(500)
            )
        )
    )
);

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();

// The scan runs while the loop runs; cancelling the token ends both
using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await foreach (IGapAdvertisement advertisement in adapter.Observer.ScanAsync(stop.Token))
    {
        string name = advertisement.Data.TryGetCompleteLocalName(out string? localName) ? localName : "-";
        Console.WriteLine(
            $"{advertisement.Timestamp:HH:mm:ss.fff} {advertisement.Address} {advertisement.Rssi} dBm {name}"
        );
    }
}
catch (OperationCanceledException) when (stop.IsCancellationRequested)
{
    Console.WriteLine("Scan stopped");
}
