using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Mock;

/// <summary> Simulated peripherals advertise into the mock adapter's scans on the manager's clock </summary>
public sealed class MockAirTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.From([
        (AdTypes.CompleteLocalName, "Sensor"u8.ToArray()),
    ]);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task AScan_SeesAPeripheralsAdvertising_EveryInterval()
    {
        var advertising = new TaskCompletionSource<AdvertisingSet>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
            )
        );
        AdvertisingSet set = await advertising.Task;
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        for (var tick = 0; tick < 3; tick++)
        {
            ValueTask<bool> next = scan.MoveNextAsync();
            _time.Advance(Interval);

            (await next).ShouldBeTrue();
            scan.Current.Address.ShouldBe(set.RandomAddress);
            scan.Current.Data.ToByteArray().ShouldBe(SensorData.ToByteArray());
            scan.Current.Timestamp.ShouldBe(_time.GetUtcNow());
        }
    }

    [Theory]
    [InlineData(ScanType.Active, true)]
    [InlineData(ScanType.Passive, false)]
    public async Task OnlyAnActiveScan_GetsTheScanResponseOfAScannableSet(ScanType scanType, bool expectScanResponse)
    {
        AdvertisingData scanResponse = AdvertisingData.Empty.WithCompleteLocalName("Details");
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (peripheral, _) =>
                    peripheral.Broadcaster.StartAdvertisingAsync(
                        SensorData,
                        Interval,
                        BleEventType.AdvScanInd,
                        scanResponse
                    )
            )
        );
        await adapter.Observer.ConfigureAsync(new BleScanParameters { ScanType = scanType }, Token);
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(QueueOptions.Unbounded, Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();

        _time.Advance(Interval * 2);

        (await first).ShouldBeTrue();
        scan.Current.EventType.ShouldBe(BleEventType.AdvScanInd);
        (await scan.MoveNextAsync()).ShouldBeTrue();
        IGapAdvertisement second = scan.Current;
        if (expectScanResponse)
        {
            second.EventType.ShouldBe(BleEventType.AdvScanInd | BleEventType.ScanResponse);
            second.Data.ToByteArray().ShouldBe(scanResponse.ToByteArray());
        }
        else
        {
            second.EventType.ShouldBe(BleEventType.AdvScanInd);
        }
    }

    [Fact]
    public async Task ChangedAdvertisingData_ShowsUpInTheNextReport()
    {
        var advertising = new TaskCompletionSource<AdvertisingSet>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
            )
        );
        AdvertisingSet set = await advertising.Task;
        AdvertisingData changed = AdvertisingData.Empty.WithCompleteLocalName("Changed");
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);

        await set.SetAdvertisingDataAsync(changed, Token);
        ValueTask<bool> first = scan.MoveNextAsync();
        _time.Advance(Interval);

        (await first).ShouldBeTrue();
        scan.Current.Data.ToByteArray().ShouldBe(changed.ToByteArray());
    }

    [Fact]
    public async Task ADisposedSet_SendsNoMoreReports()
    {
        var stopping = new TaskCompletionSource<AdvertisingSet>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options
                .AddPeripheral(
                    async (peripheral, _) =>
                        stopping.SetResult(await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval)),
                    "Stopping"
                )
                .AddPeripheral(
                    (peripheral, _) =>
                        peripheral.Broadcaster.StartAdvertisingAsync(
                            AdvertisingData.Empty.WithCompleteLocalName("Other"),
                            Interval * 2
                        ),
                    "Other"
                )
        );
        AdvertisingSet stoppingSet = await stopping.Task;
        await stoppingSet.DisposeAsync();
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();

        _time.Advance(Interval * 2);

        (await first).ShouldBeTrue();
        scan.Current.Data.TryGetCompleteLocalName(out string? name).ShouldBeTrue();
        name.ShouldBe("Other");
    }

    [Fact]
    public async Task TxPowerToRssi_DecidesTheSignalStrengthScansSee()
    {
        var parameters = new AdvertisingParameters
        {
            MinPrimaryAdvertisingInterval = Interval,
            MaxPrimaryAdvertisingInterval = Interval,
            AdvertisingTxPower = (TxPowerLevel)10,
        };
        await using BleAdapter adapter = await OpenAsync(options =>
        {
            options.TxPowerToRssi = txPower => (Rssi)((sbyte)txPower - 50);
            options.AddPeripheral(
                async (peripheral, _) =>
                {
                    AdvertisingSet set = await peripheral.Broadcaster.CreateAdvertisingSetAsync(parameters, SensorData);
                    await set.StartAsync();
                }
            );
        });
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();

        _time.Advance(Interval);

        (await first).ShouldBeTrue();
        scan.Current.Rssi.ShouldBe((Rssi)(-40));
    }

    [Fact]
    public async Task TheDefaultSignalModel_StaysWithinTheRssiRange()
    {
        var parameters = new AdvertisingParameters
        {
            MinPrimaryAdvertisingInterval = Interval,
            MaxPrimaryAdvertisingInterval = Interval,
            AdvertisingTxPower = (TxPowerLevel)(-127),
        };
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                {
                    AdvertisingSet set = await peripheral.Broadcaster.CreateAdvertisingSetAsync(parameters, SensorData);
                    await set.StartAsync();
                }
            )
        );
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();

        _time.Advance(Interval);

        (await first).ShouldBeTrue();
        scan.Current.Rssi.ShouldBe((Rssi)(-127));
    }

    [Fact]
    public async Task DisposingTheMockAdapter_DisposesItsPeripherals()
    {
        var opened = new TaskCompletionSource<BleAdapter>();
        BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (peripheral, _) =>
                {
                    opened.SetResult(peripheral);
                    return Task.CompletedTask;
                }
            )
        );
        BleAdapter peripheral = await opened.Task;

        await adapter.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task OpenAsync_WhenAPeripheralFails_DisposesTheOnesAlreadyOpened()
    {
        var first = new TaskCompletionSource<BleAdapter>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options
                        .AddPeripheral(
                            (peripheral, _) =>
                            {
                                first.SetResult(peripheral);
                                return Task.CompletedTask;
                            }
                        )
                        .AddPeripheral((_, _) => throw new InvalidOperationException("Setup failed"))
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        await Should.ThrowAsync<InvalidOperationException>(() => adapters[0].OpenAsync(Token));

        BleAdapter firstPeripheral = await first.Task;
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            firstPeripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task OpenAsync_WhenCancelledDuringThePeripheralSetup_DisposesThePeripherals()
    {
        using var cancellation = new CancellationTokenSource();
        var opened = new TaskCompletionSource<BleAdapter>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            opened.SetResult(peripheral);
                            await cancellation.CancelAsync();
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        await Should.ThrowAsync<OperationCanceledException>(() => adapters[0].OpenAsync(cancellation.Token));

        BleAdapter peripheral = await opened.Task;
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task BackgroundWork_RunsUntilTheMockAdapterCloses_WhileItsPeripheralIsStillOpen()
    {
        var started = new TaskCompletionSource();
        var endedWithPeripheralOpen = new TaskCompletionSource();
        BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (peripheral, background) =>
                {
                    background.Run(async cancellationToken =>
                    {
                        started.SetResult();
                        await Task.Delay(Timeout.InfiniteTimeSpan, _time, cancellationToken)
                            .ContinueWith(_ => { }, TaskScheduler.Default);
                        // Cleanup of the work can still use its peripheral
                        await using AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                            SensorData,
                            Interval,
                            cancellationToken: Token
                        );
                        endedWithPeripheralOpen.SetResult();
                    });
                    return Task.CompletedTask;
                }
            )
        );
        await started.Task;

        await adapter.DisposeAsync();

        endedWithPeripheralOpen.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task BackgroundWork_TicksWithTheManagersClock()
    {
        // Longer than the test waits, so only the fake clock can make it tick
        TimeSpan period = TimeSpan.FromHours(1);
        var timerStarted = new TaskCompletionSource();
        var ticked = new TaskCompletionSource();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (_, background) =>
                {
                    background.Run(async cancellationToken =>
                    {
                        using var timer = new PeriodicTimer(period, background.TimeProvider);
                        timerStarted.SetResult();
                        await timer.WaitForNextTickAsync(cancellationToken);
                        ticked.SetResult();
                    });
                    return Task.CompletedTask;
                }
            )
        );
        await timerStarted.Task;

        _time.Advance(period);

        await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    [Fact]
    public async Task FailedBackgroundWork_FailsDisposingTheMockAdapter()
    {
        var failure = new InvalidOperationException("The sensor broke");
        BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (_, background) =>
                {
                    background.Run(_ => Task.FromException(failure));
                    return Task.CompletedTask;
                }
            )
        );

        AggregateException thrown = await Should.ThrowAsync<AggregateException>(() => adapter.DisposeAsync().AsTask());

        thrown.InnerExceptions.ShouldBe([failure]);
    }

    [Fact]
    public async Task ACancellationCallbackThatThrows_StillStopsTheWorkAndDisposesThePeripheral()
    {
        var opened = new TaskCompletionSource<BleAdapter>();
        var started = new TaskCompletionSource();
        var workEnded = new TaskCompletionSource();
        BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                (peripheral, background) =>
                {
                    opened.SetResult(peripheral);
                    background.Run(async cancellationToken =>
                    {
                        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        using CancellationTokenRegistration broken = cancellationToken.Register(() =>
                        {
                            cancelled.SetResult();
                            throw new InvalidOperationException("The callback broke");
                        });
                        started.SetResult();
                        await cancelled.Task;
                        workEnded.SetResult();
                    });
                    return Task.CompletedTask;
                }
            )
        );
        await started.Task;

        await Should.ThrowAsync<AggregateException>(() => adapter.DisposeAsync().AsTask());

        workEnded.Task.IsCompletedSuccessfully.ShouldBeTrue();
        BleAdapter peripheral = await opened.Task;
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task OpenAsync_WhenSetupFailsAfterStartingWorkThatFailsToo_KeepsTheSetupsException()
    {
        var setupFailure = new InvalidOperationException("Setup failed");
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        (_, background) =>
                        {
                            background.Run(_ => Task.FromException(new InvalidOperationException("The sensor failed")));
                            throw setupFailure;
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        AggregateException thrown = await Should.ThrowAsync<AggregateException>(() => adapters[0].OpenAsync(Token));

        thrown.InnerExceptions[0].ShouldBe(setupFailure);
    }

    private async Task<BleAdapter> OpenAsync(Action<MockBackendOptions> configure)
    {
        var manager = new BleManager(builder => builder.AddMock(configure), timeProvider: _time);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
