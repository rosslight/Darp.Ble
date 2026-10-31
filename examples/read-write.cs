#:project ../src/Darp.Ble/Darp.Ble.csproj
#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj

// Connects to a peripheral, reads a typed value, writes a new one and reads it back.
// The mock backend adds a simulated peripheral that keeps the value, so this runs without Bluetooth hardware.
// Usage: dotnet run examples/read-write.cs
using Darp.Ble;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;

BleUuid serviceUuid = BleUuid.FromUInt16(0xFFF0);

// A little-endian ushort that centrals may read and write
TypedCharacteristicDeclaration<ushort, Properties.Read, Properties.Write> setpoint = CharacteristicDeclaration.Create<
    ushort,
    Properties.Read,
    Properties.Write
>(0xFFF1);

ushort stored = 20;
var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            async (simulated, _) =>
            {
                var service = new LocalGattServiceBuilder(serviceUuid);
                service.AddCharacteristic(
                    setpoint,
                    onRead: (_, _) => ValueTask.FromResult(stored),
                    onWrite: (connection, value, _) =>
                    {
                        Console.WriteLine($"Peripheral: {connection.Address} wrote {value}");
                        stored = value;
                        return ValueTask.FromResult(GattProtocolStatus.Success);
                    }
                );
                await simulated.Peripheral.AddServiceAsync(service);
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

RemoteGattService service = await connection.DiscoverServiceAsync(serviceUuid, timeout.Token);

// Only the operations the declaration's markers allow compile on the result
TypedRemoteCharacteristic<ushort, Properties.Read, Properties.Write> remote = await service.DiscoverCharacteristicAsync(
    setpoint,
    timeout.Token
);
Console.WriteLine($"Setpoint: {await remote.ReadAsync(timeout.Token)}");
await remote.WriteAsync((ushort)22, timeout.Token);
Console.WriteLine($"Setpoint after the write: {await remote.ReadAsync(timeout.Token)}");
return 0;
