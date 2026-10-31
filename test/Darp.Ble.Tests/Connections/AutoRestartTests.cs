using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> A connection stops the advertising set it came through; autoRestart starts it again afterwards </summary>
public sealed class AutoRestartTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.Empty.WithCompleteLocalName("Sensor");

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task WithAutoRestart_TheSetAdvertisesAgain_AfterTheCentralDisconnected()
    {
        (BleAdapter adapter, BleAdapter peripheral, AdvertisingSet set) = await OpenAsync(autoRestart: true);
        await using BleAdapter owned = adapter;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();

        (await Should.ThrowAsync<BleConnectException>(() => ConnectAsync(adapter, set))).Kind.ShouldBe(
            BleConnectErrorKind.Unreachable
        );
        await connection.DisposeAsync();
        await central.Disconnected.WithWatchdog();

        IGapAdvertisement advertisement = await NextAdvertisementAsync(adapter);
        advertisement.Address.ShouldBe(set.RandomAddress);
    }

    [Fact]
    public async Task WithoutAutoRestart_TheSetStaysStopped_UntilItIsStartedAgain()
    {
        (BleAdapter adapter, _, AdvertisingSet set) = await OpenAsync(autoRestart: false);
        await using BleAdapter owned = adapter;
        ConnectedPeripheral connection = await ConnectAsync(adapter, set);
        await connection.DisposeAsync();

        await set.StartAsync(Token);

        await using ConnectedPeripheral again = await ConnectAsync(adapter, set);
    }

    [Fact]
    public async Task DisposingTheSet_WhileItWaitsForTheCentral_EndsTheWaitForTheRestart()
    {
        (BleAdapter adapter, BleAdapter peripheral, AdvertisingSet set) = await OpenAsync(autoRestart: true);
        await using BleAdapter owned = adapter;
        ConnectedPeripheral connection = await ConnectAsync(adapter, set);

        await set.DisposeAsync();

        peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();
        await connection.DisposeAsync();
        (await Should.ThrowAsync<BleConnectException>(() => ConnectAsync(adapter, set))).Kind.ShouldBe(
            BleConnectErrorKind.Unreachable
        );
    }

    private static Task<ConnectedPeripheral> ConnectAsync(BleAdapter adapter, AdvertisingSet set) =>
        adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: Token);

    /// <summary> Scans until an advertisement arrives, moving the clock on by one interval at a time </summary>
    private async Task<IGapAdvertisement> NextAdvertisementAsync(BleAdapter adapter)
    {
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> next = scan.MoveNextAsync();
        // The restart runs after the disconnect, off the caller's thread
        await Watchdog.UntilAsync(() => next.IsCompleted, step: () => _time.Advance(Interval));
        (await next).ShouldBeTrue();
        return scan.Current;
    }

    private async Task<(BleAdapter Adapter, BleAdapter Peripheral, AdvertisingSet Set)> OpenAsync(bool autoRestart)
    {
        var advertising = new TaskCompletionSource<(BleAdapter, AdvertisingSet)>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                            advertising.SetResult(
                                (
                                    peripheral,
                                    await peripheral.Broadcaster.StartAdvertisingAsync(
                                        SensorData,
                                        Interval,
                                        autoRestart: autoRestart
                                    )
                                )
                            )
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        BleAdapter adapter = await adapters[0].OpenAsync(Token);
        (BleAdapter peripheralAdapter, AdvertisingSet set) = await advertising.Task;
        return (adapter, peripheralAdapter, set);
    }
}
