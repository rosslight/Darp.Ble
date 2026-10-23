using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> How a mock peripheral runs its hosted handlers per central </summary>
public sealed class HandlerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);
    private static readonly BleUuid ValueUuid = BleUuid.FromUInt16(0x2A19);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, BleAdapter OtherCentral, AdvertisingSet Set)> _devices =
        new();
    private OnReadHandler _onRead = (_, _) => ValueTask.FromResult<byte[]>([]);
    private OnWriteHandler _onWrite = (_, _, _) => ValueTask.FromResult(GattProtocolStatus.Success);
    private LocalGattCharacteristic? _hosted;

    [Fact]
    public async Task AHandlersToken_IsCancelledWhenItsCentralDisconnects()
    {
        var started = new TaskCompletionSource();
        var returned = new TaskCompletionSource();
        _onWrite = async (_, _, ct) =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException)
            {
                returned.SetResult();
            }
            return GattProtocolStatus.Success;
        };
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, ConnectedCentral central) = await ConnectAsync(adapter);
        Task write = characteristic.WriteAsync(new byte[] { 1 }, Token);
        await started.Task;

        await central.DisconnectAsync(Token);

        await returned.Task;
        // The handler returned after its cancellation, so its answer races the link going down; the write ends either way
        await write.ContinueWith(_ => { }, TaskScheduler.Default);
    }

    [Fact]
    public async Task AHandler_CanAwaitDisconnectingItsOwnCentral_AndTheHostStillWaitsForItToReturn()
    {
        var disconnected = new TaskCompletionSource<DisconnectInfo>();
        var release = new TaskCompletionSource();
        var returned = new TaskCompletionSource();
        // Turns a disconnect that waits for its own handler into a failure instead of a hang
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _onRead = async (central, _) =>
        {
            await central.DisconnectAsync(guard.Token);
            disconnected.SetResult(await central.Disconnected);
            await release.Task;
            returned.SetResult();
            return [];
        };
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, _) = await ConnectAsync(adapter);
        (BleAdapter host, _, _) = await _devices.Task;
        Task read = characteristic.ReadAsync(Token);

        DisconnectInfo info = await disconnected.Task.WaitAsync(guard.Token);

        info.Reason.ShouldBe(DisconnectReason.LocalRequest);
        // Taken while the handler still runs; asserted once it returned, so a failure cannot hang the test
        IReadOnlyList<ConnectedCentral> listed = host.Peripheral.ConnectedCentrals;
        Task disposing = host.DisposeAsync().AsTask();
        disposing.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await disposing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, Token);
        returned.Task.IsCompleted.ShouldBeTrue();
        await read.ContinueWith(_ => { }, TaskScheduler.Default);
        // Gone for the app once the link is down, though its handler still ran
        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandlersOfOneCentral_RunOneAtATime()
    {
        var events = new List<string>();
        var firstRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _onWrite = async (_, value, ct) =>
        {
            byte write = value.Span[0];
            lock (events)
                events.Add($"{write} entered");
            if (write == 1)
            {
                firstRunning.SetResult();
                await releaseFirst.Task.WaitAsync(ct);
            }
            lock (events)
                events.Add($"{write} returned");
            return GattProtocolStatus.Success;
        };
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, _) = await ConnectAsync(adapter);

        // The command's handler holds the central's turn; the request behind it must wait for it
        await characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);
        await firstRunning.Task.WithWatchdog();
        Task request = characteristic.WriteAsync(new byte[] { 2 }, Token);
        releaseFirst.SetResult();
        await request.WithWatchdog();

        events.ShouldBe(["1 entered", "1 returned", "2 entered", "2 returned"]);
    }

    [Fact]
    public async Task AHandlerThatBlocksForOneCentral_DoesNotDelayAnotherCentral()
    {
        var blocked = new TaskCompletionSource<byte[]>();
        var blocking = new TaskCompletionSource();
        await using BleAdapter adapter = await OpenAsync();
        _onRead = (central, _) =>
        {
            if (central.Address != adapter.RandomAddress)
                return ValueTask.FromResult<byte[]>([2]);
            blocking.SetResult();
            return new ValueTask<byte[]>(blocked.Task);
        };
        (RemoteGattCharacteristic first, _) = await ConnectAsync(adapter);
        (_, BleAdapter otherCentral, AdvertisingSet set) = await _devices.Task;
        Task<byte[]> blockedRead = first.ReadAsync(Token);
        await blocking.Task;
        await set.StartAsync(Token);
        RemoteGattCharacteristic second = await DiscoverAsync(otherCentral, set.RandomAddress);

        (await second.ReadAsync(Token)).ShouldBe([2]);

        blockedRead.IsCompleted.ShouldBeFalse();
        blocked.SetResult([1]);
        (await blockedRead).ShouldBe([1]);
    }

    [Fact]
    public async Task NotifyAsync_ReachesOnlyTheCentralItNames()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic first, ConnectedCentral firstCentral) = await ConnectAsync(adapter);
        (BleAdapter host, BleAdapter otherCentral, AdvertisingSet set) = await _devices.Task;
        await set.StartAsync(Token);
        RemoteGattCharacteristic second = await DiscoverAsync(otherCentral, set.RandomAddress);
        ConnectedCentral secondCentral = host.Peripheral.ConnectedCentrals.Single(c => c != firstCentral);
        using NotificationStream<byte[]> firstStream = first.Subscribe();
        using NotificationStream<byte[]> secondStream = second.Subscribe();
        await first.EnableNotificationsAsync(Token);
        await second.EnableNotificationsAsync(Token);

        await Hosted.NotifyAsync(firstCentral, new byte[] { 1 }, Token);
        await Hosted.NotifyAsync(secondCentral, new byte[] { 2 }, Token);

        (await firstStream.FirstAsync(Token)).ShouldBe([1]);
        (await secondStream.FirstAsync(Token)).ShouldBe([2]);
    }

    [Fact]
    public async Task DisposingThePeripheral_EndsEveryCentral_BeforeWaitingForAnyHandler()
    {
        var firstStarted = new TaskCompletionSource();
        var secondStarted = new TaskCompletionSource();
        var secondCancelled = new TaskCompletionSource();
        var calls = 0;
        _onRead = async (_, ct) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                firstStarted.SetResult();
                // Ignores its own token and only returns once the other central's handler was cancelled
                await secondCancelled.Task;
            }
            else
            {
                secondStarted.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    secondCancelled.SetResult();
                }
            }
            return [];
        };
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic first, _) = await ConnectAsync(adapter);
        (BleAdapter host, BleAdapter otherCentral, AdvertisingSet set) = await _devices.Task;
        Task<byte[]> firstRead = first.ReadAsync(Token);
        await firstStarted.Task;
        await set.StartAsync(Token);
        RemoteGattCharacteristic second = await DiscoverAsync(otherCentral, set.RandomAddress);
        Task<byte[]> secondRead = second.ReadAsync(Token);
        await secondStarted.Task;

        await host.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, Token);

        host.Peripheral.ConnectedCentrals.ShouldBeEmpty();
        // Both handlers returned, so their answers race their links going down; the reads end either way
        await Task.WhenAll(firstRead, secondRead).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private LocalGattCharacteristic Hosted => _hosted ?? throw new InvalidOperationException("Not hosted");

    private async Task<(RemoteGattCharacteristic Characteristic, ConnectedCentral Central)> ConnectAsync(
        BleAdapter adapter
    )
    {
        (BleAdapter host, _, AdvertisingSet set) = await _devices.Task;
        RemoteGattCharacteristic characteristic = await DiscoverAsync(adapter, set.RandomAddress);
        return (characteristic, host.Peripheral.ConnectedCentrals.Single(c => c.Address == adapter.RandomAddress));
    }

    private static async Task<RemoteGattCharacteristic> DiscoverAsync(BleAdapter central, BleAddress address)
    {
        ConnectedPeripheral connection = await central.Central.ConnectAsync(address, cancellationToken: Token);
        RemoteGattService service = await connection.DiscoverServiceAsync(ServiceUuid, Token);
        return (await service.DiscoverCharacteristicsAsync(Token)).Single();
    }

    /// <summary> Opens the mock adapter with a host that serves one characteristic and a second device that can connect to it as well </summary>
    private async Task<BleAdapter> OpenAsync()
    {
        var host = new TaskCompletionSource<(BleAdapter, AdvertisingSet)>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options
                        .AddPeripheral(
                            async (peripheral, _) =>
                            {
                                var service = new LocalGattServiceBuilder(ServiceUuid);
                                _hosted = service.AddCharacteristic(
                                    ValueUuid,
                                    GattProperty.Read
                                        | GattProperty.Write
                                        | GattProperty.WriteWithoutResponse
                                        | GattProperty.Notify,
                                    onRead: (central, ct) => _onRead(central, ct),
                                    onWrite: (central, value, ct) => _onWrite(central, value, ct)
                                );
                                await peripheral.Peripheral.AddServiceAsync(service);
                                host.SetResult(
                                    (
                                        peripheral,
                                        await peripheral.Broadcaster.StartAdvertisingAsync(
                                            AdvertisingData.Empty,
                                            Interval
                                        )
                                    )
                                );
                            },
                            "Host"
                        )
                        .AddPeripheral(
                            async (otherCentral, _) =>
                            {
                                (BleAdapter hostAdapter, AdvertisingSet set) = await host.Task;
                                _devices.SetResult((hostAdapter, otherCentral, set));
                            },
                            "Other central"
                        )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
