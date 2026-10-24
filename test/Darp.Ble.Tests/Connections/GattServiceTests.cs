using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> The GATT service a mock peripheral serves itself, through the public API </summary>
public sealed class GattServiceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly BleUuid GattService = BleUuid.FromUInt16(0x1801);
    private static readonly BleUuid DatabaseHash = BleUuid.FromUInt16(0x2B2A);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<BleAdapter> _peripheral = new();
    private readonly TaskCompletionSource<BleAddress> _address = new();

    [Fact]
    public async Task APeripheralWithoutTheGattService_OffersOnlyGapAndItsOwnServices()
    {
        await using BleAdapter adapter = await OpenAsync(includeGattService: false);
        await using ConnectedPeripheral connection = await ConnectAsync(adapter);

        IReadOnlyList<RemoteGattService> services = await connection.Gatt.DiscoverServicesAsync(Token);

        services.Select(service => service.Uuid).ShouldBe([BleUuid.FromUInt16(0x1800), BleUuid.FromUInt16(0x180F)]);
    }

    [Fact]
    public async Task TheDatabaseHash_ChangesOnceThePeripheralAddsAService()
    {
        await using BleAdapter adapter = await OpenAsync(includeGattService: true);
        await using ConnectedPeripheral connection = await ConnectAsync(adapter);
        RemoteGattService gatt = await connection.Gatt.DiscoverServiceAsync(GattService, Token);
        RemoteGattCharacteristic hash = (await gatt.DiscoverCharacteristicsAsync(Token)).Single(c =>
            c.Uuid == DatabaseHash
        );

        byte[] before = await hash.ReadAsync(Token);
        byte[] again = await hash.ReadAsync(Token);
        BleAdapter peripheral = await _peripheral.Task;
        await peripheral.Peripheral.Gatt.AddServiceAsync(new LocalGattServiceBuilder(0x180A), Token);
        byte[] after = await hash.ReadAsync(Token);

        before.Length.ShouldBe(16);
        again.ShouldBe(before);
        after.ShouldNotBe(before);
    }

    [Fact]
    public async Task PublishingAnotherGattService_Throws()
    {
        await using BleAdapter adapter = await OpenAsync(includeGattService: true);
        BleAdapter peripheral = await _peripheral.Task;

        await Should.ThrowAsync<InvalidOperationException>(() =>
            peripheral.Peripheral.Gatt.AddServiceAsync(new LocalGattServiceBuilder(0x1801), Token)
        );
    }

    private async Task<ConnectedPeripheral> ConnectAsync(BleAdapter adapter) =>
        await adapter.Central.ConnectAsync(await _address.Task, cancellationToken: Token);

    /// <summary> Opens the mock adapter with one peripheral that hosts a battery service and advertises </summary>
    private async Task<BleAdapter> OpenAsync(bool includeGattService)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async peripheral =>
                        {
                            var service = new LocalGattServiceBuilder(0x180F);
                            service.AddCharacteristic(0x2A19, GattProperty.Read);
                            await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                            AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                                AdvertisingData.Empty,
                                TimeSpan.FromMilliseconds(100)
                            );
                            _peripheral.SetResult(peripheral);
                            _address.SetResult(set.RandomAddress);
                        },
                        includeGattService: includeGattService
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
