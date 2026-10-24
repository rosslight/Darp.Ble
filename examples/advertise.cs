#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Advertises a name until Ctrl+C.
// With the mock backend nobody outside this program sees it; the code is the same for a real adapter.
// Usage: dotnet run examples/advertise.cs
using Darp.Ble;
using Darp.Ble.Gap;
using Darp.Ble.Mock;

var manager = new BleManager(builder => builder.AddMock());
IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

// Disposing the set stops advertising
await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Darp.Ble example"),
    interval: TimeSpan.FromMilliseconds(100)
);
Console.WriteLine($"Advertising as {advertising.RandomAddress}; press Ctrl+C to stop");

try
{
    await Task.Delay(Timeout.Infinite, stop.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Advertising stopped");
}
