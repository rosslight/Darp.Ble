using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Tests.Lifecycle;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Darp.Ble.Tests.Scanning;

/// <summary> How scans share the radio (D6) and how their queues behave (D2), at the driver boundary </summary>
public sealed class BleObserverTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly TestObserverDriver _driver = new();

    [Fact]
    public async Task TwoScans_EachGetEveryReport_AndShareOneRadioScan()
    {
        await using BleAdapter adapter = await OpenAsync();
        ScanSession first = await StartScanAsync(adapter.Observer.ScanAsync(Token));
        ScanSession second = await StartScanAsync(adapter.Observer.ScanAsync(Token), skipWarmUpOf: first);

        _driver.Report(Adv(1), Adv(2));

        (await first.TakeAsync(2)).ShouldBe([Adv(1), Adv(2)]);
        (await second.TakeAsync(2)).ShouldBe([Adv(1), Adv(2)]);
        _driver.Starts.Count.ShouldBe(1);
        await first.DisposeAsync();
        _driver.StopCount.ShouldBe(0);
        await second.DisposeAsync();
        _driver.StopCount.ShouldBe(1);
    }

    [Fact]
    public async Task FailMode_EndsOnlyThatScan_AfterItsQueuedReports()
    {
        await using BleAdapter adapter = await OpenAsync();
        ScanSession failing = await StartScanAsync(
            adapter.Observer.ScanAsync(new QueueOptions(2, QueueFullMode.Fail), Token)
        );
        ScanSession other = await StartScanAsync(adapter.Observer.ScanAsync(Token), skipWarmUpOf: failing);

        _driver.Report(Adv(1), Adv(2), Adv(3));

        (await failing.TakeAsync(2)).ShouldBe([Adv(1), Adv(2)]);
        BleQueueOverflowException exception = await Should.ThrowAsync<BleQueueOverflowException>(failing.MoveNextAsync);
        exception.Capacity.ShouldBe(2);
        (await other.TakeAsync(3)).ShouldBe([Adv(1), Adv(2), Adv(3)]);
        adapter.Observer.DroppedAdvertisementCount.ShouldBe(1);
        _driver.IsScanning.ShouldBeTrue();
        await failing.DisposeAsync();
        await other.DisposeAsync();
    }

    [Theory]
    [InlineData(QueueFullMode.DropOldest, new byte[] { 2, 3 })]
    [InlineData(QueueFullMode.DropNewest, new byte[] { 1, 2 })]
    public async Task DropModes_KeepTheirEndOfTheQueue_AndCountTheDrops(QueueFullMode mode, byte[] kept)
    {
        await using BleAdapter adapter = await OpenAsync();
        await using ScanSession scan = await StartScanAsync(
            adapter.Observer.ScanAsync(new QueueOptions(2, mode), Token)
        );

        _driver.Report(Adv(1), Adv(2), Adv(3));

        (await scan.TakeAsync(2)).ShouldBe(kept.Select(Adv).ToArray());
        adapter.Observer.DroppedAdvertisementCount.ShouldBe(1);
    }

    [Fact]
    public async Task UnboundedQueue_KeepsEveryReport()
    {
        await using BleAdapter adapter = await OpenAsync();
        await using ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(QueueOptions.Unbounded, Token));
        IGapAdvertisement[] reports = [.. Enumerable.Range(1, 200).Select(n => Adv((byte)n))];

        _driver.Report(reports);

        (await scan.TakeAsync(200)).ShouldBe(reports);
        adapter.Observer.DroppedAdvertisementCount.ShouldBe(0);
    }

    [Fact]
    public async Task RadioFailure_DeliversTheQueuedReportsFirst_ThenTheError()
    {
        await using BleAdapter adapter = await OpenAsync();
        ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(Token));
        var failure = new BleException("Controller reset");

        _driver.Report(Adv(1));
        _driver.Fail(failure);

        (await scan.TakeAsync(1)).ShouldBe([Adv(1)]);
        (await Should.ThrowAsync<BleException>(scan.MoveNextAsync)).ShouldBeSameAs(failure);
        await scan.DisposeAsync();
        _driver.StopCount.ShouldBe(0); // the radio stopped by itself
        await using ScanSession next = await StartScanAsync(adapter.Observer.ScanAsync(Token));
        _driver.Starts.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConfigureAsync_WhileScanning_RestartsTheRadioScan_AndIgnoresTheOldRun()
    {
        var loggerFactory = new RecordingLoggerFactory();
        await using BleAdapter adapter = await OpenAsync(loggerFactory);
        await using ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(Token));
        var oldRun = _driver.Sink;
        var parameters = new BleScanParameters
        {
            ScanType = ScanType.Active,
            ScanWindow = TimeSpan.FromMilliseconds(30),
        };

        await adapter.Observer.ConfigureAsync(parameters, Token);
        oldRun.OnReport(Adv(1));
        oldRun.OnFailed(new BleException("Old run failed"));
        _driver.Report(Adv(2));

        (await scan.TakeAsync(1)).ShouldBe([Adv(2)]);
        _driver.Starts[^1].ShouldBe(parameters);
        _driver.StopCount.ShouldBe(1);
        adapter.Observer.Parameters.ShouldBe(parameters);
        loggerFactory.Entries.ShouldContain(entry => entry.EventId.Id == 1102 && entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ConfigureAsync_WhileNotScanning_AppliesToTheNextScan()
    {
        await using BleAdapter adapter = await OpenAsync();
        var parameters = new BleScanParameters { ScanType = ScanType.Active };

        await adapter.Observer.ConfigureAsync(parameters, Token);
        await using ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(Token));

        _driver.Starts.ShouldBe([parameters]);
    }

    [Fact]
    public async Task ConfigureAsync_WithAWindowLongerThanTheInterval_Throws()
    {
        await using BleAdapter adapter = await OpenAsync();
        var parameters = new BleScanParameters
        {
            ScanInterval = TimeSpan.FromMilliseconds(50),
            ScanWindow = TimeSpan.FromMilliseconds(100),
        };

        await Should.ThrowAsync<ArgumentException>(() => adapter.Observer.ConfigureAsync(parameters, Token));
    }

    [Fact]
    public async Task ScanAsync_WhenTheRadioCannotStart_Throws_AndTheNextScanTriesAgain()
    {
        await using BleAdapter adapter = await OpenAsync();
        var failure = new BleException("No radio");
        _driver.StartFailure = failure;

        (
            await Should.ThrowAsync<BleException>(() => adapter.Observer.ScanAsync(Token).FirstAsync(Token).AsTask())
        ).ShouldBeSameAs(failure);
        _driver.StartFailure = null;
        await using ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(Token));

        _driver.Starts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ScanAsync_WhenCancelled_EndsTheScanAndStopsTheRadio()
    {
        await using BleAdapter adapter = await OpenAsync();
        using var cancellation = new CancellationTokenSource();
        IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(cancellation.Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> next = scan.MoveNextAsync();

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(next.AsTask());
        await scan.DisposeAsync();
        _driver.StopCount.ShouldBe(1);
    }

    [Fact]
    public async Task DisposingTheAdapter_EndsItsScansAndStopsTheRadio()
    {
        BleAdapter adapter = await OpenAsync();
        ScanSession scan = await StartScanAsync(adapter.Observer.ScanAsync(Token));

        await adapter.DisposeAsync();

        (await scan.MoveNextAsync()).ShouldBeFalse();
        await scan.DisposeAsync();
        _driver.StopCount.ShouldBe(1);
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            adapter.Observer.ScanAsync(Token).FirstAsync(Token).AsTask()
        );
    }

    [Fact]
    public async Task Observer_WhenTheAdapterCannotScan_ThrowsNotSupportedException()
    {
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(new TestAdapterDriver())));
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        BleAdapter adapter = await adapters[0].OpenAsync(Token);

        Should.Throw<NotSupportedException>(() => adapter.Observer);
    }

    private async Task<BleAdapter> OpenAsync(ILoggerFactory? loggerFactory = null)
    {
        var manager = new BleManager(
            builder => builder.AddBackend(new TestBackend(new TestScanAdapterDriver(_driver))),
            loggerFactory
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }

    /// <summary> Starts a scan; the warm-up report also reaches a scan that already runs, which reads it here </summary>
    private async Task<ScanSession> StartScanAsync(
        IAsyncEnumerable<IGapAdvertisement> scan,
        ScanSession? skipWarmUpOf = null
    )
    {
        ScanSession session = await ScanSession.StartAsync(scan, _driver, Adv(0));
        if (skipWarmUpOf is not null)
            await skipWarmUpOf.TakeAsync(1);
        return session;
    }

    private static readonly Dictionary<byte, IGapAdvertisement> Advertisements = [];

    /// <summary> One advertisement per number, the same instance each time, so assertions can compare references </summary>
    private static IGapAdvertisement Adv(byte number)
    {
        lock (Advertisements)
        {
            if (!Advertisements.TryGetValue(number, out IGapAdvertisement? advertisement))
            {
                advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
                    DateTimeOffset.UnixEpoch,
                    BleEventType.AdvNonConnInd,
                    new BleAddress(BleAddressType.Public, (UInt48)number),
                    Physical.Le1M,
                    Physical.NotAvailable,
                    AdvertisingSId.NoAdIProvided,
                    TxPowerLevel.NotAvailable,
                    (Rssi)(-50),
                    PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                    BleAddress.NotAvailable,
                    AdvertisingData.Empty
                );
                Advertisements[number] = advertisement;
            }
            return advertisement;
        }
    }
}
