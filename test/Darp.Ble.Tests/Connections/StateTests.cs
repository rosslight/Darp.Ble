using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Per-connection state of a central through the Mock </summary>
public sealed class StateTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly ServiceDeclaration Service = new(0xFFF0);
    private static readonly CharacteristicDeclaration<Properties.Write> Command = new(0xFFF1);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set)> _host = new();
    private OnWriteHandler _onWrite = (_, _, _) => ValueTask.FromResult(GattProtocolStatus.Success);

    [Fact]
    public async Task AState_IsOnePerTypeAndCentral_AndIsDisposedOnceAfterTheRunningHandlerReturned()
    {
        var events = new List<string>();
        var handlerRunning = new TaskCompletionSource();
        var releaseHandler = new TaskCompletionSource();
        ConnectedCentral? seen = null;
        _onWrite = async (central, value, _) =>
        {
            Session session = central.GetOrAddState(() => new Session(events));
            session.Writes++;
            seen = central;
            if (value.Span[0] == 2)
            {
                handlerRunning.SetResult();
                // Ignores its token: the state must outlive the cancellation until this returns
                await releaseHandler.Task;
                lock (events)
                    events.Add("handler returned");
            }
            return GattProtocolStatus.Success;
        };
        await using BleAdapter adapter = await OpenAsync();
        RemoteCharacteristic<Properties.Write> command = await ConnectAsync(adapter);
        await command.WriteAsync(new byte[] { 1 }, Token);
        Task second = command.WriteAsync(new byte[] { 2 }, Token);
        await handlerRunning.Task;
        ConnectedCentral central = seen ?? throw new InvalidOperationException("No handler ran");

        await central.DisconnectAsync(Token);
        // The link is down, but the handler still runs, so its state is still there
        central.GetOrAddState(() => new Session(events)).Writes.ShouldBe(2);
        releaseHandler.SetResult();
        // The host's disposal waits until the handlers returned and the states were disposed
        await (await _host.Task).Host.DisposeAsync();

        events.ShouldBe(["handler returned", "disposed async"]);
        Should.Throw<ObjectDisposedException>(() => central.GetOrAddState(() => new Session(events)));
        await second.ContinueWith(_ => { }, TaskScheduler.Default);
    }

    [Fact]
    public async Task AFailingFactory_LeavesNoState()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedCentral central = await ConnectCentralAsync(adapter);

        Should.Throw<InvalidOperationException>(() =>
            central.GetOrAddState<Session>(() => throw new InvalidOperationException("Factory failed"))
        );
        var created = new Session([]);

        central.GetOrAddState(() => created).ShouldBeSameAs(created);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AFailingDisposal_DoesNotStopTheStatesAfterIt(bool failsAsync)
    {
        var events = new List<string>();
        await using BleAdapter adapter = await OpenAsync();
        ConnectedCentral central = await ConnectCentralAsync(adapter);
        // Added first, so the states after it are disposed only if the cleanup goes on after the failure
        if (failsAsync)
            central.GetOrAddState(() => new FailingAsyncDisposal());
        else
            central.GetOrAddState(() => new FailingDisposal());
        central.GetOrAddState(() => new AsyncState(events));
        central.GetOrAddState(() => new SyncState(events));

        // The host's disposal waits until the states were disposed
        await (await _host.Task).Host.DisposeAsync().AsTask().WithWatchdog();

        events.ShouldBe(["async state disposed", "state disposed"], ignoreOrder: true);
    }

    /// <summary> Connects and returns the host's side of the connection, as the first handler saw it </summary>
    private async Task<ConnectedCentral> ConnectCentralAsync(BleAdapter adapter)
    {
        ConnectedCentral? seen = null;
        _onWrite = (central, _, _) =>
        {
            seen = central;
            return ValueTask.FromResult(GattProtocolStatus.Success);
        };
        RemoteCharacteristic<Properties.Write> command = await ConnectAsync(adapter);
        await command.WriteAsync(new byte[] { 1 }, Token);
        return seen ?? throw new InvalidOperationException("No handler ran");
    }

    private async Task<RemoteCharacteristic<Properties.Write>> ConnectAsync(BleAdapter adapter)
    {
        (_, AdvertisingSet set) = await _host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.DiscoverServiceAsync(Service, Token);
        return await service.DiscoverCharacteristicAsync(Command, Token);
    }

    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            var service = new LocalGattServiceBuilder(Service);
                            service.AddCharacteristic(
                                Command,
                                onWrite: (central, value, ct) => _onWrite(central, value, ct)
                            );
                            await peripheral.Peripheral.AddServiceAsync(service);
                            _host.SetResult(
                                (
                                    peripheral,
                                    await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                                )
                            );
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }

    /// <summary> A state that can be disposed both ways; the library prefers the async one </summary>
    private sealed class Session(List<string> events) : IAsyncDisposable, IDisposable
    {
        private readonly List<string> _events = events;

        public int Writes { get; set; }

        public ValueTask DisposeAsync()
        {
            lock (_events)
                _events.Add("disposed async");
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            lock (_events)
                _events.Add("disposed");
        }
    }

    private sealed class AsyncState(List<string> events) : IAsyncDisposable
    {
        private readonly List<string> _events = events;

        public ValueTask DisposeAsync()
        {
            lock (_events)
                _events.Add("async state disposed");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SyncState(List<string> events) : IDisposable
    {
        private readonly List<string> _events = events;

        public void Dispose()
        {
            lock (_events)
                _events.Add("state disposed");
        }
    }

    private sealed class FailingDisposal : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("Dispose failed");
    }

    private sealed class FailingAsyncDisposal : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.FromException(new InvalidOperationException("Dispose failed"));
    }
}
