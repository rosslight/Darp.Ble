using System.Collections.Concurrent;
using System.Globalization;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.SerilogTracing;
using Microsoft.Extensions.Time.Testing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SerilogTracing;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> SerilogTracing renders the library's spans with their templates (seam 1 of PR 5) </summary>
[Collection(TelemetryCollection.Name)]
public sealed class SerilogTracingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task ASpan_IsRenderedWithItsTemplate_FilledFromItsTags()
    {
        var sink = new CollectingSink();
        using Logger logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();
        using IDisposable listener = new ActivityListenerConfiguration()
            .Instrument.With<DarpBleActivityInstrumentor>()
            .TraceTo(logger);
        var host = new TaskCompletionSource<AdvertisingSet>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        var service = new LocalGattServiceBuilder(ServiceUuid);
                        service.AddCharacteristic(
                            0x2A19,
                            GattProperty.Read,
                            (_, _) => ValueTask.FromResult<byte[]>([50])
                        );
                        await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                        host.SetResult(
                            await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                        );
                    })
                ),
            timeProvider: _time
        );
        await using BleAdapter adapter = await (await manager.EnumerateAdaptersAsync(Token))[0].OpenAsync(Token);
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            (await host.Task).RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(ServiceUuid, Token);

        await (await service.DiscoverCharacteristicsAsync(Token)).Single().ReadAsync(Token);

        LogEvent read = sink.Events.Single(e =>
            string.Equals(e.MessageTemplate.Text, "Read {Uuid}", StringComparison.Ordinal)
        );
        // Serilog quotes strings unless the output template says :l, as {Message:lj} does
        read.RenderMessage(CultureInfo.InvariantCulture).ShouldBe("Read \"2A19\"");
        sink.Events.Select(e => e.RenderMessage(CultureInfo.InvariantCulture))
            .ShouldContain(rendered =>
                string.Equals(rendered, $"Open adapter \"{BleBackends.Mock}\"", StringComparison.Ordinal)
            );
    }

    [Theory]
    [InlineData("Darp.Ble", true)]
    [InlineData("Darp.Ble.HciHost", true)]
    [InlineData("Darp.Bleeding", false)]
    [InlineData("Other", false)]
    public void TheInstrumentor_SubscribesToTheLibrarysSources_Only(string source, bool expected) =>
        new DarpBleActivityInstrumentor().ShouldSubscribeTo(source).ShouldBe(expected);

    private sealed class CollectingSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> _events = new();

        public IReadOnlyList<LogEvent> Events => [.. _events];

        public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
    }
}
