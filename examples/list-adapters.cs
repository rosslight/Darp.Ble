#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Lists the adapters of the registered backends.
// Usage: dotnet run examples/list-adapters.cs
using Darp.Ble;
using Darp.Ble.Mock;

var manager = new BleManager(builder => builder.AddMock());

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
foreach (BleAdapterInfo adapter in adapters)
    Console.WriteLine($"{adapter.Backend}: {adapter.Name}");
