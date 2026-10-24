#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Hosts a service, advertises it and prints the centrals that are connected, until Ctrl+C.
// With the mock backend nobody outside this program connects; the code is the same for a real adapter.
// Usage: dotnet run examples/host-service.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;

BleUuid serviceUuid = BleUuid.FromUInt16(0x180D);

var manager = new BleManager(builder => builder.AddMock());
IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

// Centrals can discover the service once AddServiceAsync returns
var service = new LocalGattServiceBuilder(serviceUuid);
service.AddCharacteristic(BleUuid.FromUInt16(0x2A37), GattProperty.Notify);
await adapter.Peripheral.Gatt.AddServiceAsync(service, stop.Token);

// A connection stops the set; autoRestart advertises again once that central disconnected
await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Darp.Ble example").WithCompleteListOfServiceUuids([serviceUuid]),
    interval: TimeSpan.FromMilliseconds(100),
    autoRestart: true
);
Console.WriteLine($"Hosting {serviceUuid} as {advertising.RandomAddress}; press Ctrl+C to stop");

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
try
{
    int connected = 0;
    while (await timer.WaitForNextTickAsync(stop.Token))
    {
        IReadOnlyList<ConnectedCentral> centrals = adapter.Peripheral.ConnectedCentrals;
        if (centrals.Count == connected)
            continue;
        connected = centrals.Count;
        Console.WriteLine($"Connected centrals: {string.Join(", ", centrals.Select(central => central.Address))}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Stopped hosting");
}
