using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> The core's metrics, through the Mock </summary>
[Collection(TelemetryCollection.Name)]
public sealed class MetricTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set, LocalGattCharacteristic Level)> _host =
        new();
    private OnReadHandler _onRead = (_, _) => ValueTask.FromResult<byte[]>([50]);
    private OnWriteHandler _onWrite = (_, _, _) => ValueTask.FromResult(GattProtocolStatus.Success);

    [Fact]
    public async Task ARequest_RecordsItsSecondsFromEnqueueToItsEnd_WithItsOperationAndOutcome()
    {
        var readRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readAnswer = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeAnswer = new TaskCompletionSource<GattProtocolStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _onRead = (_, _) =>
        {
            readRunning.SetResult();
            return new ValueTask<byte[]>(readAnswer.Task);
        };
        _onWrite = (_, _, _) =>
        {
            writeRunning.SetResult();
            return new ValueTask<GattProtocolStatus>(writeAnswer.Task);
        };
        using var measurements = new RecordedMeasurements();
        await using BleAdapter adapter = await OpenAsync();
        RemoteGattCharacteristic level = await ConnectAsync(adapter);

        Task<byte[]> read = level.ReadAsync(Token);
        await readRunning.Task.WithWatchdog();
        _time.Advance(TimeSpan.FromSeconds(2));
        // Both wait behind the read: the write runs after it, the second read is given up while it waits
        Task write = level.WriteAsync(new byte[] { 1 }, Token);
        using var giveUp = new CancellationTokenSource();
        Task<byte[]> givenUp = level.ReadAsync(giveUp.Token);
        _time.Advance(TimeSpan.FromSeconds(1));
        await giveUp.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(givenUp.WithWatchdog());
        _time.Advance(TimeSpan.FromSeconds(2));
        readAnswer.SetResult([50]);
        await read.WithWatchdog();
        await writeRunning.Task.WithWatchdog();
        _time.Advance(TimeSpan.FromSeconds(4));
        writeAnswer.SetResult(GattProtocolStatus.WriteRequestRejected);
        await Should.ThrowAsync<GattException>(write.WithWatchdog());

        const string duration = "darp.ble.gatt.client.request.duration";
        measurements
            .Of(
                duration,
                ("darp.ble.backend", BleBackends.Mock),
                ("darp.ble.operation", "read"),
                ("darp.ble.outcome", "success")
            )
            .ShouldBe([5]);
        measurements.Of(duration, ("darp.ble.operation", "read"), ("darp.ble.outcome", "cancelled")).ShouldBe([1]);
        measurements.Of(duration, ("darp.ble.operation", "write"), ("darp.ble.outcome", "att_error")).ShouldBe([7]);
        measurements.Of(duration, ("darp.ble.operation", "exchange_mtu")).Count.ShouldBe(1);
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
                                GattProperty.Read | GattProperty.Write | GattProperty.Notify,
                                onRead: (central, ct) => _onRead(central, ct),
                                onWrite: (central, value, ct) => _onWrite(central, value, ct)
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
