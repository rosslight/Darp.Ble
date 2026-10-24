using System.Reflection;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Tests.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> Log events and the diagnostics options through the public API (seam 3 of PR 5, D9, D14) </summary>
[Collection(TelemetryCollection.Name)]
public sealed class LogTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public void EveryLogEvent_HasAUniqueIdAndName_ThatTheCatalogLists()
    {
        LoggerMessageAttribute[] events =
        [
            .. typeof(BleManager)
                .Assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>())
                .OfType<LoggerMessageAttribute>(),
        ];
        string catalog = File.ReadAllText(FindCatalog());

        events.Select(e => e.EventId).ShouldBeUnique();
        events.Select(e => e.EventName).Distinct(StringComparer.Ordinal).Count().ShouldBe(events.Length);
        events.ShouldAllBe(e => catalog.Contains($"| {e.EventId} | {e.EventName} |", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AHandlerStillRunning5sAfterItsCentralStartedDisconnecting_IsLoggedOnce()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var release = new TaskCompletionSource<GattProtocolStatus>();
        var running = new TaskCompletionSource();
        var host = new TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(
            loggerFactory,
            host,
            onWrite: async (_, _, _) =>
            {
                running.SetResult();
                // Ignores its token
                return await release.Task;
            }
        );
        (BleAdapter peripheral, AdvertisingSet set) = await host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(ServiceUuid, Token);
        RemoteGattCharacteristic command = (await service.DiscoverCharacteristicsAsync(Token)).Single();
        Task write = command.WriteAsync(new byte[] { 1 }, Token);
        await running.Task;
        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.Single();

        Task disconnecting = central.DisconnectAsync(Token);
        _time.Advance(TimeSpan.FromSeconds(4.9));
        SlowHandlerWarnings(loggerFactory).ShouldBe(0);
        _time.Advance(TimeSpan.FromSeconds(0.1));
        SlowHandlerWarnings(loggerFactory).ShouldBe(1);
        _time.Advance(TimeSpan.FromSeconds(10));
        release.SetResult(GattProtocolStatus.Success);
        await disconnecting;

        SlowHandlerWarnings(loggerFactory).ShouldBe(1);
        await write.ContinueWith(_ => { }, TaskScheduler.Default);
    }

    [Fact]
    public async Task WithoutAddresses_LogsAndSpansHideThePeer_AndAnotherManagerStillShowsIt()
    {
        using var spans = new RecordedSpans();
        var hidden = new RecordingLoggerFactory();
        var hiddenHost = new TaskCompletionSource<(BleAdapter, AdvertisingSet)>();
        await using BleAdapter hiding = await OpenAsync(
            hidden,
            hiddenHost,
            configure: options => options.IncludeAddresses = false
        );
        var shown = new RecordingLoggerFactory();
        var shownHost = new TaskCompletionSource<(BleAdapter, AdvertisingSet)>();
        await using BleAdapter showing = await OpenAsync(shown, shownHost);
        AdvertisingSet hiddenSet = (await hiddenHost.Task).Item2;
        AdvertisingSet shownSet = (await shownHost.Task).Item2;

        await hiding.Central.ConnectAsync(hiddenSet.RandomAddress, cancellationToken: Token);
        await showing.Central.ConnectAsync(shownSet.RandomAddress, cancellationToken: Token);

        // A failure's message may name the peer too: logs and spans leave it out
        await Should.ThrowAsync<BleConnectException>(() =>
            hiding.Central.ConnectAsync(BleAddress.NewRandomStaticAddress(), cancellationToken: Token)
        );
        hidden.Entries.Single(entry => entry.EventId.Id == 1302).Exception.ShouldBeNull();
        spans
            .All.Single(span =>
                string.Equals(span.DisplayName, "Connect", StringComparison.Ordinal)
                && span.Status == System.Diagnostics.ActivityStatusCode.Error
            )
            .StatusDescription.ShouldBe("BleConnectException");
        // The simulated peripheral follows the manager's options
        hidden.Entries.Single(entry => entry.EventId.Id == 1400).Message.ShouldBe("Central <hidden> connected");
        Connecting(hidden).ShouldBe("Connecting to <hidden>");
        Connecting(shown).ShouldBe($"Connecting to {shownSet.RandomAddress}");
        string?[] addresses =
        [
            .. spans
                .All.Where(span =>
                    string.Equals(span.DisplayName, "Connect", StringComparison.Ordinal)
                    && span.Status != System.Diagnostics.ActivityStatusCode.Error
                )
                .Select(span => (string?)span.GetTagItem("darp.ble.address")),
        ];
        addresses.ShouldBe(["<hidden>", shownSet.RandomAddress.ToString()], ignoreOrder: true);
    }

    [Fact]
    public async Task ASlowDriverDisconnect_IsNoSlowHandlerWarning_WhenNoHandlerRuns()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var driver = new Connections.TestPeripheralDriver { HoldDisconnect = new TaskCompletionSource() };
        var manager = new BleManager(
            builder =>
                builder.AddBackend(
                    new TestBackend(
                        new TestAdapterDriver
                        {
                            Capabilities = Capabilities.Peripheral,
                            Broadcaster = new Advertising.TestBroadcasterDriver(),
                            Peripheral = driver,
                        }
                    )
                ),
            loggerFactory,
            _time
        );
        await using BleAdapter adapter = await (await manager.EnumerateAdaptersAsync(Token))[0].OpenAsync(Token);
        var builder = new LocalGattServiceBuilder(ServiceUuid);
        builder.AddCharacteristic(0x2A19, GattProperty.Notify);
        await adapter.Peripheral.Gatt.AddServiceAsync(builder, Token);
        driver.ConnectSubscribed(1);
        ConnectedCentral central = adapter.Peripheral.ConnectedCentrals.Single();

        Task disconnecting = central.DisconnectAsync(Token);
        _time.Advance(TimeSpan.FromSeconds(6));
        driver.HoldDisconnect.SetResult();
        await disconnecting;

        SlowHandlerWarnings(loggerFactory).ShouldBe(0);
    }

    private static string Connecting(RecordingLoggerFactory loggerFactory) =>
        loggerFactory.Entries.First(entry => entry.EventId.Id == 1300).Message;

    private static int SlowHandlerWarnings(RecordingLoggerFactory loggerFactory) =>
        loggerFactory.Entries.Count(entry => entry.EventId.Id == 1405 && entry.Level == LogLevel.Warning);

    private static string FindCatalog()
    {
        for (
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            string candidate = Path.Combine(directory.FullName, "docs", "telemetry.md");
            if (File.Exists(candidate))
                return candidate;
        }
        throw new FileNotFoundException("docs/telemetry.md not found above the test binaries");
    }

    private async Task<BleAdapter> OpenAsync(
        RecordingLoggerFactory loggerFactory,
        TaskCompletionSource<(BleAdapter, AdvertisingSet)> host,
        OnWriteHandler? onWrite = null,
        Action<Darp.Ble.Diagnostics.BleDiagnosticsOptions>? configure = null
    )
    {
        var manager = new BleManager(
            builder =>
            {
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        var service = new LocalGattServiceBuilder(ServiceUuid);
                        service.AddCharacteristic(
                            0x2A19,
                            GattProperty.Write,
                            onWrite: onWrite ?? ((_, _, _) => ValueTask.FromResult(GattProtocolStatus.Success))
                        );
                        await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                        host.SetResult(
                            (
                                peripheral,
                                await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                            )
                        );
                    })
                );
                if (configure is not null)
                    builder.ConfigureDiagnostics(configure);
            },
            loggerFactory,
            _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
