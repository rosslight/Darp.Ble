using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> The core's metrics, through the Mock (seam 2 of PR 5, D14) </summary>
[Collection(TelemetryCollection.Name)]
public sealed class MetricTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set, LocalGattCharacteristic Level)> _host =
        new();

    [Fact]
    public async Task ARequest_RecordsItsDuration_WithItsOperationAndOutcome()
    {
        using var measurements = new RecordedMeasurements();
        await using BleAdapter adapter = await OpenAsync();
        RemoteGattCharacteristic level = await ConnectAsync(adapter);

        await level.ReadAsync(Token);
        await Should.ThrowAsync<OperationCanceledException>(() => level.ReadAsync(new CancellationToken(true)));

        const string duration = "darp.ble.gatt.client.request.duration";
        measurements.Of(duration, ("darp.ble.operation", "read"), ("darp.ble.outcome", "success")).Count.ShouldBe(1);
        measurements.Of(duration, ("darp.ble.operation", "read"), ("darp.ble.outcome", "cancelled")).Count.ShouldBe(1);
        measurements.Of(duration, ("darp.ble.operation", "exchange_mtu")).Count.ShouldBe(1);
        measurements.Of(duration, ("darp.ble.backend", BleBackends.Mock)).ShouldAllBe(seconds => seconds >= 0);
    }

    [Fact]
    public async Task DroppedItems_AreCounted_ForScansAndForNotifications()
    {
        using var measurements = new RecordedMeasurements();
        await using BleAdapter adapter = await OpenAsync();
        await using (
            IAsyncEnumerator<IGapAdvertisement> scan = adapter
                .Observer.ScanAsync(new QueueOptions(1, QueueFullMode.DropNewest), Token)
                .GetAsyncEnumerator(Token)
        )
        {
            ValueTask<bool> first = scan.MoveNextAsync();
            _time.Advance(Interval);
            (await first).ShouldBeTrue();
            // Three more reports while nobody reads: the queue keeps one and drops two
            for (var i = 0; i < 3; i++)
                _time.Advance(Interval);
        }
        RemoteGattCharacteristic level = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = level.Subscribe(new QueueOptions(1, QueueFullMode.Fail));
        await level.EnableNotificationsAsync(Token);
        (BleAdapter host, _, LocalGattCharacteristic hosted) = await _host.Task;
        ConnectedCentral central = host.Peripheral.ConnectedCentrals.Single();
        await hosted.NotifyAsync(central, new byte[] { 1 }, Token);
        await hosted.NotifyAsync(central, new byte[] { 2 }, Token);

        measurements.Of("darp.ble.queue.dropped", ("darp.ble.queue", "advertisements")).Sum().ShouldBe(2);
        measurements.Of("darp.ble.queue.dropped", ("darp.ble.queue", "notifications")).Sum().ShouldBe(1);
    }

    [Fact]
    public async Task OpenConnections_AreCountedUpAndDown_OnBothSides()
    {
        using var measurements = new RecordedMeasurements();
        await using BleAdapter adapter = await OpenAsync();
        await ConnectAsync(adapter);
        ConnectedPeripheral connection = adapter.Central.Connections.Single();
        ConnectedCentral central = (await _host.Task).Host.Peripheral.ConnectedCentrals.Single();

        await connection.DisposeAsync();
        await central.Disconnected;

        const string count = "darp.ble.connection.count";
        measurements.Of(count, ("darp.ble.role", "central")).ShouldBe([1, -1]);
        measurements.Of(count, ("darp.ble.role", "peripheral")).ShouldBe([1, -1]);
    }

    private async Task<RemoteGattCharacteristic> ConnectAsync(BleAdapter adapter)
    {
        (_, AdvertisingSet set, _) = await _host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.DiscoverServiceAsync(ServiceUuid, Token);
        return (await service.DiscoverCharacteristicsAsync(Token)).Single();
    }

    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            var service = new LocalGattServiceBuilder(ServiceUuid);
                            LocalGattCharacteristic level = service.AddCharacteristic(
                                0x2A19,
                                GattProperty.Read | GattProperty.Notify,
                                onRead: (_, _) => ValueTask.FromResult<byte[]>([50])
                            );
                            await peripheral.Peripheral.AddServiceAsync(service);
                            AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                                AdvertisingData.Empty,
                                Interval
                            );
                            _host.SetResult((peripheral, set, level));
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
