using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Darp.Ble.Tests.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Mock;

/// <summary> A simulated peripheral that leaves range loses its links as on a supervision timeout, and is heard again once it enters </summary>
public sealed class MockRangeTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.Empty.WithCompleteLocalName("Sensor");

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Device, AdvertisingSet Set)> _sensor = new();

    [Fact]
    public async Task Leave_EndsTheAppsConnectionToThePeripheral_WithConnectionTimeoutOnBothSides()
    {
        var range = new MockRange();
        await using BleAdapter adapter = await OpenAsync(range);
        (BleAdapter sensor, AdvertisingSet set) = await _sensor.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        ConnectedCentral central = sensor.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();

        range.Leave();

        range.IsInRange.ShouldBeFalse();
        (await connection.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.ConnectionTimeout);
        (await central.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.ConnectionTimeout);
    }

    [Fact]
    public async Task APeripheralOutOfRange_IsNeitherHeardNorReachable_UntilItEnters()
    {
        var range = new MockRange();
        // Out of range before the mock adapter opens, so it opens out of range
        range.Leave();
        await using BleAdapter adapter = await OpenAsync(
            range,
            options =>
                options.AddPeripheral(
                    (other, _) =>
                        other.Broadcaster.StartAdvertisingAsync(
                            AdvertisingData.Empty.WithCompleteLocalName("Other"),
                            Interval * 2
                        ),
                    "Other"
                )
        );
        (_, AdvertisingSet set) = await _sensor.Task;
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(QueueOptions.Unbounded, Token)
            .GetAsyncEnumerator(Token);

        ValueTask<bool> first = scan.MoveNextAsync();
        // The sensor's events are due before the other's first one, so a scan that heard them would report them first
        _time.Advance(Interval * 2);

        (await first.AsTask().WithWatchdog()).ShouldBeTrue();
        scan.Current.Address.ShouldNotBe(set.RandomAddress);
        BleConnectException unreachable = await Should.ThrowAsync<BleConnectException>(() =>
            adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: Token)
        );
        unreachable.Kind.ShouldBe(BleConnectErrorKind.Unreachable);

        range.Enter();
        ValueTask<bool> next = scan.MoveNextAsync();
        // Only the sensor's next event is due
        _time.Advance(Interval);

        (await next.AsTask().WithWatchdog()).ShouldBeTrue();
        scan.Current.Address.ShouldBe(set.RandomAddress);
        await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        adapter.Central.Connections.ShouldBe([connection]);
    }

    [Fact]
    public async Task Leave_EndsTheConnectionOfASimulatedCentral_WithConnectionTimeoutOnBothSides()
    {
        var range = new MockRange();
        await using BleAdapter adapter = await OpenAsync(range);
        (BleAdapter device, _) = await _sensor.Task;
        await using AdvertisingSet hosting = await adapter.Broadcaster.StartAdvertisingAsync(
            SensorData,
            Interval,
            cancellationToken: Token
        );
        ConnectedPeripheral connection = await device.Central.ConnectAsync(
            hosting.RandomAddress,
            cancellationToken: Token
        );
        ConnectedCentral central = adapter.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();

        range.Leave();

        (await central.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.ConnectionTimeout);
        (await connection.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.ConnectionTimeout);
    }

    [Fact]
    public async Task ASimulatedCentralThatLeavesWhileItsConnectionIsAccepted_LosesThatConnection()
    {
        var range = new MockRange();
        // Leaves the moment the app's peripheral admits the central, before its connect returned
        var logs = new RecordingLoggerFactory(onLog: entry =>
        {
            if (string.Equals(entry.EventId.Name, "CentralConnected", StringComparison.Ordinal))
                range.Leave();
        });
        await using BleAdapter adapter = await OpenAsync(range, loggerFactory: logs);
        (BleAdapter device, _) = await _sensor.Task;
        await using AdvertisingSet hosting = await adapter.Broadcaster.StartAdvertisingAsync(
            SensorData,
            Interval,
            cancellationToken: Token
        );

        BleDisconnectedException lost = await Should.ThrowAsync<BleDisconnectedException>(() =>
            device.Central.ConnectAsync(hosting.RandomAddress, cancellationToken: Token).WithWatchdog()
        );

        lost.Info.Reason.ShouldBe(DisconnectReason.ConnectionTimeout);
        device.Central.Connections.ShouldBeEmpty();
        await Watchdog.UntilAsync(() => adapter.Peripheral.ConnectedCentrals.Count == 0);
    }

    /// <summary> Opens the mock adapter with a sensor in the range that advertises connectable </summary>
    private async Task<BleAdapter> OpenAsync(
        MockRange range,
        Action<MockBackendOptions>? configure = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                {
                    options.AddPeripheral(
                        async (sensor, _) =>
                            _sensor.SetResult(
                                (sensor, await sensor.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                            ),
                        "Sensor",
                        range: range
                    );
                    configure?.Invoke(options);
                }),
            loggerFactory,
            _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
