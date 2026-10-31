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
>(BleUuid.FromUInt16(0xFFF1));

var stored = new byte[] { 20, 0 };
var manager = new BleManager(builder =>
    builder.AddMock(mock =>
        mock.AddPeripheral(
            async (simulated, _) =>
            {
                var service = new LocalGattServiceBuilder(serviceUuid);
                service.AddCharacteristic(
                    setpoint.Uuid,
                    setpoint.Properties,
                    onRead: (_, _) => ValueTask.FromResult(stored),
                    onWrite: (central, value, _) =>
                    {
                        Console.WriteLine($"Peripheral: {central.Address} wrote {Convert.ToHexString(value.Span)}");
                        stored = value.ToArray();
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
