#:project ../src/Darp.Ble/Darp.Ble.csproj
#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Hosts an echo service on a simulated peripheral and sends it requests from the central side.
// The peripheral answers each request by notifying the reply to the central that asked.
// Usage: dotnet run examples/request-response.cs
using System.Text;
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock;

BleUuid serviceUuid = BleUuid.FromUInt16(0xFD4C);
BleUuid requestUuid = BleUuid.FromUInt16(0xFF71);
BleUuid replyUuid = BleUuid.FromUInt16(0xFF72);

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            async (simulated, _) =>
            {
                // The handler runs for one request of one central at a time; its token ends with that central's connection
                await simulated.Peripheral.AddEchoServiceAsync(
                    serviceUuid,
                    requestUuid,
                    replyUuid,
                    (connection, request, _) =>
                        ValueTask.FromResult(
                            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(request.Span).ToUpperInvariant())
                        )
                );
                await simulated.Broadcaster.StartAdvertisingAsync(
                    AdvertisingData.Empty.WithCompleteListOfServiceUuids([serviceUuid]),
                    interval: TimeSpan.FromMilliseconds(100)
                );
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
IGapAdvertisement advertisement = await adapter
    .Observer.ScanAsync(timeout.Token)
    .WhereConnectable()
    .WhereService(serviceUuid)
    .FirstAsync(timeout.Token);
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
    advertisement.Address,
    cancellationToken: timeout.Token
);
RemoteEchoService echo = await connection.DiscoverEchoServiceAsync(serviceUuid, requestUuid, replyUuid, timeout.Token);

foreach (string request in new[] { "hello", "darp.ble" })
{
    // Subscribes before writing, so a reply sent at once is not lost; throws TimeoutException without one
    byte[] reply = await echo.QueryOneAsync(Encoding.UTF8.GetBytes(request), TimeSpan.FromSeconds(2), timeout.Token);
    Console.WriteLine($"{request} -> {Encoding.UTF8.GetString(reply)}");
}
return 0;
