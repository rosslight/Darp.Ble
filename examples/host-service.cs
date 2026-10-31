#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Hosts a chat service, advertises it and prints each central that connects or disconnects, until Ctrl+C.
// A central writes a message; the host numbers it per central and notifies it to every subscribed central.
// The mock backend adds a simulated central that sends two messages and disconnects, so this runs without Bluetooth hardware.
// Usage: dotnet run examples/host-service.cs
using System.Text;
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using HostService;

BleUuid serviceUuid = BleUuid.FromUInt16(0xFFF0);
TypedCharacteristicDeclaration<string, Properties.Write> message = CharacteristicDeclaration.Create<Properties.Write>(
    0xFFF1,
    Encoding.UTF8
);
TypedCharacteristicDeclaration<string, Properties.Notify> feed = CharacteristicDeclaration.Create<Properties.Notify>(
    0xFFF2,
    Encoding.UTF8
);

var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            (simulated, background) =>
            {
                // The simulated device uses its central role: it finds the host, subscribes to the feed and writes
                background.Run(async cancellationToken =>
                {
                    IGapAdvertisement host = await simulated
                        .Observer.ScanAsync(cancellationToken)
                        .WhereConnectable()
                        .WhereService(serviceUuid)
                        .FirstAsync(cancellationToken);
                    await using ConnectedPeripheral connection = await simulated.Central.ConnectAsync(
                        host.Address,
                        cancellationToken: cancellationToken
                    );
                    RemoteGattService service = await connection.DiscoverServiceAsync(serviceUuid, cancellationToken);
                    TypedRemoteCharacteristic<string, Properties.Notify> remoteFeed =
                        await service.DiscoverCharacteristicAsync(feed, cancellationToken);
                    TypedRemoteCharacteristic<string, Properties.Write> remoteMessage =
                        await service.DiscoverCharacteristicAsync(message, cancellationToken);
                    using NotificationStream<string> stream = remoteFeed.Subscribe();
                    await remoteFeed.EnableNotificationsAsync(cancellationToken);
                    string[] texts = ["hello", "bye"];
                    foreach (string text in texts)
                    {
                        await remoteMessage.WriteAsync(text, cancellationToken);
                        Console.WriteLine($"Simulated central got: {await stream.FirstAsync(cancellationToken)}");
                    }
                });
                return Task.CompletedTask;
            },
            name: "Simulated central"
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

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

// Centrals can discover the service once AddServiceAsync returns
var chat = new LocalGattServiceBuilder(serviceUuid);
TypedLocalCharacteristic<string, Properties.Notify> localFeed = chat.AddCharacteristic(feed);
chat.AddCharacteristic(
    message,
    onWrite: async (central, text, cancellationToken) =>
    {
        // State per central, disposed after its connection ended; handlers of one central run one at a time
        ChatSession session = central.GetOrAddState(() => new ChatSession());
        session.Messages++;
        await localFeed.NotifyAllAsync($"{central.Address} #{session.Messages}: {text}", cancellationToken);
        return GattProtocolStatus.Success;
    }
);
await adapter.Peripheral.AddServiceAsync(chat, stop.Token);

// A connection stops the set; autoRestart advertises again once that central disconnected
await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Darp.Ble example").WithCompleteListOfServiceUuids([serviceUuid]),
    interval: TimeSpan.FromMilliseconds(100),
    autoRestart: true
);
Console.WriteLine($"Hosting {serviceUuid} as {advertising.RandomAddress}; press Ctrl+C to stop");

try
{
    // The centrals connected now, then each new one; Disconnected completes once it leaves
    await foreach (ConnectedCentral central in adapter.Peripheral.WatchCentralsAsync(stop.Token))
    {
        Console.WriteLine($"{central.Address} connected");
        _ = central.Disconnected.ContinueWith(
            task => Console.WriteLine($"{central.Address} disconnected: {task.Result.Reason}"),
            TaskScheduler.Default
        );
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Stopped hosting");
}
return 0;

namespace HostService
{
    /// <summary> What the host keeps per central </summary>
    internal sealed class ChatSession
    {
        public int Messages { get; set; }
    }
}
