#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Opens an adapter and prints the roles it supports.
// Usage: dotnet run examples/open-adapter.cs [adapter-name]
using Darp.Ble;
using Darp.Ble.Mock;

var manager = new BleManager(builder => builder.AddMock());

IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
string? requestedName = args.Length > 0 ? args[0] : null;
BleAdapterInfo? info = adapters.FirstOrDefault(a =>
    requestedName is null || string.Equals(a.Name, requestedName, StringComparison.Ordinal)
);
if (info is null)
{
    Console.Error.WriteLine("No adapter found");
    return 1;
}

try
{
    // The adapter is ready to use, or opening throws. Disposing it closes the adapter
    await using BleAdapter adapter = await info.OpenAsync();
    Console.WriteLine($"Opened {adapter.Name} ({adapter.Backend}), roles: {adapter.Capabilities}");
    return 0;
}
catch (BleOpenException e)
{
    // Kind says why: NotAvailable, Disabled or MissingPermission
    Console.Error.WriteLine($"Could not open {info.Name}: {e.Kind}, {e.Message}");
    return 1;
}
