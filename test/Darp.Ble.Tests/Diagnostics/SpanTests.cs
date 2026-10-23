using System.Diagnostics;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> The spans the core owns, through the Mock (seam 1 of PR 5, D14) </summary>
[Collection(TelemetryCollection.Name)]
public sealed class SpanTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);
    private static readonly BleUuid LevelUuid = BleUuid.FromUInt16(0x2A19);

    private static readonly ActivitySource CallerSource = new("Darp.Ble.Tests.Caller");

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<AdvertisingSet> _host = new();

    [Fact]
    public async Task OpeningConnectingAndReading_AreSpans_WithTheirOutcome_AndRequestsBelongToTheirCaller()
    {
        using var spans = new RecordedSpans();
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await _host.Task;
        using ActivityListener callerListener = ListenTo(CallerSource.Name);

        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattCharacteristic level = await DiscoverAsync(connection);
        Activity root = CallerSource.StartActivity("read") ?? throw new InvalidOperationException("Not sampled");
        await level.ReadAsync(Token);
        root.Stop();
        await Should.ThrowAsync<GattException>(() => level.WriteAsync(new byte[] { 1 }, Token));

        // The mock adapter and its simulated peripheral both open
        Activity[] opens =
        [
            .. spans.All.Where(span =>
                string.Equals(span.DisplayName, "Open adapter {Backend}", StringComparison.Ordinal)
            ),
        ];
        opens.Length.ShouldBe(2);
        opens.ShouldAllBe(span =>
            string.Equals((string?)span.GetTagItem("darp.ble.backend"), BleBackends.Mock, StringComparison.Ordinal)
        );
        opens.ShouldAllBe(span =>
            string.Equals((string?)span.GetTagItem("darp.ble.outcome"), "success", StringComparison.Ordinal)
        );
        Activity connect = spans.Single("Connect");
        connect.GetTagItem("darp.ble.address").ShouldBe(set.RandomAddress.ToString());
        connect.GetTagItem("darp.ble.outcome").ShouldBe("success");
        // The MTU exchange is the connection's first request, part of connecting
        spans.Single("Exchange MTU").ParentSpanId.ShouldBe(connect.SpanId);
        Activity read = spans.Single("Read {Uuid}");
        read.ParentSpanId.ShouldBe(root.SpanId);
        read.GetTagItem("darp.ble.uuid").ShouldBe(FormattableString.Invariant($"{LevelUuid}"));
        read.GetTagItem("darp.ble.outcome").ShouldBe("success");
        read.Status.ShouldBe(ActivityStatusCode.Unset);
        // The characteristic does not allow writes, so the write never reached the queue: no span
        spans.All.ShouldNotContain(span => string.Equals(span.DisplayName, "Write {Uuid}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnAttError_FailsTheRequestSpan_AndACancelledRequestIsNoError()
    {
        using var spans = new RecordedSpans();
        await using BleAdapter adapter = await OpenAsync(
            onRead: (_, _) => throw new GattException(GattProtocolStatus.InsufficientAuthentication, "Pair first")
        );
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            (await _host.Task).RandomAddress,
            cancellationToken: Token
        );
        RemoteGattCharacteristic level = await DiscoverAsync(connection);

        await Should.ThrowAsync<GattException>(() => level.ReadAsync(Token));
        await Should.ThrowAsync<OperationCanceledException>(() => level.ReadAsync(new CancellationToken(true)));

        Activity[] reads =
        [
            .. spans.All.Where(span => string.Equals(span.DisplayName, "Read {Uuid}", StringComparison.Ordinal)),
        ];
        reads.Length.ShouldBe(2);
        reads[0].Status.ShouldBe(ActivityStatusCode.Error);
        reads[0].GetTagItem("darp.ble.outcome").ShouldBe("att_error");
        reads[1].Status.ShouldBe(ActivityStatusCode.Unset);
        reads[1].GetTagItem("darp.ble.outcome").ShouldBe("cancelled");
        // The peripheral's handler run is a span of its own
        Activity handle = spans.All.First(span =>
            string.Equals(span.DisplayName, "Handle {Operation} {Uuid}", StringComparison.Ordinal)
        );
        handle.GetTagItem("darp.ble.operation").ShouldBe("read");
        handle.GetTagItem("darp.ble.outcome").ShouldBe("att_error");
    }

    [Fact]
    public async Task AScan_IsOneSpanForItsWholeEnumeration()
    {
        using var spans = new RecordedSpans();
        await using BleAdapter adapter = await OpenAsync();
        await using (
            IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(Token).GetAsyncEnumerator(Token)
        )
        {
            for (var i = 0; i < 3; i++)
            {
                ValueTask<bool> next = scan.MoveNextAsync();
                _time.Advance(Interval);
                (await next).ShouldBeTrue();
            }
        }

        Activity scanSpan = spans.Single("Scan");
        scanSpan.GetTagItem("darp.ble.outcome").ShouldBe("success");
        spans.All.Count(span => string.Equals(span.DisplayName, "Scan", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public async Task AScanWhoseRadioCannotStop_EndsItsSpanWithTheFailure()
    {
        using var spans = new RecordedSpans();
        var observer = new Scanning.TestObserverDriver();
        var manager = new BleManager(builder =>
            builder.AddBackend(
                new Lifecycle.TestBackend(
                    new Lifecycle.TestAdapterDriver { Capabilities = Capabilities.Observer, Observer = observer }
                )
            )
        );
        await using BleAdapter adapter = await (await manager.EnumerateAdaptersAsync(Token))[0].OpenAsync(Token);
        IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(Token).GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();
        observer.Report(Scanning.TestAdvertisements.Get(1));
        (await first).ShouldBeTrue();
        observer.StopFailure = new BleException("The radio does not stop");

        await Should.ThrowAsync<BleException>(() => scan.DisposeAsync().AsTask());

        Activity scanSpan = spans.Single("Scan");
        scanSpan.Status.ShouldBe(ActivityStatusCode.Error);
        scanSpan.GetTagItem("darp.ble.outcome").ShouldBe("error");
        // The adapter's disposal stops the radio again
        observer.StopFailure = null;
    }

    [Fact]
    public async Task ACancelledScanWhoseRadioCannotStop_EndsItsSpanWithTheFailure()
    {
        using var spans = new RecordedSpans();
        var observer = new Scanning.TestObserverDriver();
        var manager = new BleManager(builder =>
            builder.AddBackend(
                new Lifecycle.TestBackend(
                    new Lifecycle.TestAdapterDriver { Capabilities = Capabilities.Observer, Observer = observer }
                )
            )
        );
        await using BleAdapter adapter = await (await manager.EnumerateAdaptersAsync(Token))[0].OpenAsync(Token);
        using var cancel = new CancellationTokenSource();
        IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(cancel.Token).GetAsyncEnumerator(Token);
        ValueTask<bool> pending = scan.MoveNextAsync();
        observer.StopFailure = new BleException("The radio does not stop");

        try
        {
            await cancel.CancelAsync();

            await Should.ThrowAsync<BleException>(() => pending.AsTask());
            Activity scanSpan = spans.Single("Scan");
            scanSpan.Status.ShouldBe(ActivityStatusCode.Error);
            scanSpan.GetTagItem("darp.ble.outcome").ShouldBe("error");
        }
        finally
        {
            // The adapter's disposal stops the radio again
            observer.StopFailure = null;
            await scan.DisposeAsync();
        }
    }

    private static ActivityListener ListenTo(string source)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => string.Equals(s.Name, source, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static async Task<RemoteGattCharacteristic> DiscoverAsync(ConnectedPeripheral connection)
    {
        RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(ServiceUuid, Token);
        return (await service.DiscoverCharacteristicsAsync(Token)).Single();
    }

    private async Task<BleAdapter> OpenAsync(OnReadHandler? onRead = null)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        var service = new LocalGattServiceBuilder(ServiceUuid);
                        service.AddCharacteristic(
                            LevelUuid,
                            GattProperty.Read,
                            onRead ?? ((_, _) => ValueTask.FromResult<byte[]>([50]))
                        );
                        await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                        _host.SetResult(
                            await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                        );
                    })
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
