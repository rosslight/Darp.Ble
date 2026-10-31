using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Discovering what a mock peripheral hosts through the public API </summary>
public sealed class DiscoveryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180D);
    private static readonly BleUuid MeasurementUuid = BleUuid.FromUInt16(0x2A37);
    private static readonly BleUuid ControlPointUuid = BleUuid.FromUInt16(0x2A39);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<BleAddress> _peripheralAddress = new();

    [Fact]
    public async Task Discovery_FindsTheGapAndGattServicesAndTheServicesAPeripheralHosts()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        IReadOnlyList<RemoteGattService> services = await connection.DiscoverServicesAsync(Token);

        // The peripheral's own GAP and GATT services come first, as on a real device
        services.Select(s => s.Uuid).ShouldBe([BleUuid.FromUInt16(0x1800), BleUuid.FromUInt16(0x1801), ServiceUuid]);
        RemoteGattService service = services[2];
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);
        characteristics
            .Select(c => (c.Uuid, c.Properties))
            .ShouldBe([(MeasurementUuid, GattProperty.Notify), (ControlPointUuid, GattProperty.Write)]);
    }

    [Fact]
    public async Task DiscoverServiceAsync_FindsAServiceByItsUuid()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        RemoteGattService service = await connection.DiscoverServiceAsync(ServiceUuid, Token);

        service.Uuid.ShouldBe(ServiceUuid);
    }

    [Fact]
    public async Task Discovery_MatchesAUuidWrittenInAnotherWidth()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        // The 128-bit form of a 16-bit UUID, as a UUID string from a data sheet gives it
        RemoteGattService service = await connection.DiscoverServiceAsync(BleUuid.FromGuid(ServiceUuid.Value), Token);
        RemoteCharacteristic<Properties.Notify> measurement = await service.DiscoverCharacteristicAsync(
            new CharacteristicDeclaration<Properties.Notify>(BleUuid.FromGuid(MeasurementUuid.Value)),
            Token
        );

        service.Uuid.ShouldBe(ServiceUuid);
        measurement.Uuid.ShouldBe(MeasurementUuid);
    }

    [Fact]
    public async Task DiscoverServiceAsync_ForAServiceThePeripheralDoesNotHost_FailsAsNotFound()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        GattException exception = await Should.ThrowAsync<GattException>(() =>
            connection.DiscoverServiceAsync(BleUuid.FromUInt16(0x180F), Token)
        );

        exception.Kind.ShouldBe(GattErrorKind.NotFound);
    }

    private async Task<ConnectedPeripheral> ConnectAsync(BleAdapter adapter) =>
        await adapter.Central.ConnectAsync(await _peripheralAddress.Task, cancellationToken: Token);

    /// <summary> Opens the mock adapter with one peripheral that hosts a heart rate service and advertises </summary>
    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            var service = new LocalGattServiceBuilder(ServiceUuid);
                            service.AddCharacteristic(MeasurementUuid, GattProperty.Notify);
                            service.AddCharacteristic(ControlPointUuid, GattProperty.Write);
                            await peripheral.Peripheral.AddServiceAsync(service);
                            AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                                AdvertisingData.Empty,
                                Interval
                            );
                            _peripheralAddress.SetResult(set.RandomAddress);
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
