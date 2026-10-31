using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Darp.Ble.Tests.Advertising;
using Darp.Ble.Tests.Lifecycle;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> NotifyAllAsync to the centrals that turned notifications on </summary>
public sealed class NotifyAllTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly ServiceDeclaration Service = new(0x180F);

    private static readonly TypedCharacteristicDeclaration<byte, Properties.Notify> Level =
        CharacteristicDeclaration.Create<byte, Properties.Notify>(0x2A19);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task NotifyAllAsync_ReachesTheCentralsWithNotificationsOn_AndSkipsTheOthers()
    {
        var hosted =
            new TaskCompletionSource<(
                BleAdapter Host,
                BleAdapter Other,
                AdvertisingSet Set,
                TypedLocalCharacteristic<byte, Properties.Notify> Level
            )>();
        await using BleAdapter adapter = await OpenMockAsync(hosted);
        (
            BleAdapter host,
            BleAdapter other,
            AdvertisingSet set,
            TypedLocalCharacteristic<byte, Properties.Notify> level
        ) = await hosted.Task;
        TypedRemoteCharacteristic<byte, Properties.Notify> first = await DiscoverAsync(adapter, set);
        await set.StartAsync(Token);
        TypedRemoteCharacteristic<byte, Properties.Notify> second = await DiscoverAsync(other, set);
        using NotificationStream<byte> firstValues = first.Subscribe();
        using NotificationStream<byte> secondValues = second.Subscribe();
        await first.EnableNotificationsAsync(Token);

        await level.NotifyAllAsync((byte)1, Token);
        await second.EnableNotificationsAsync(Token);
        await first.DisableNotificationsAsync(Token);
        await level.NotifyAllAsync((byte)2, Token);

        (await firstValues.FirstAsync(Token).AsTask().WithWatchdog()).ShouldBe((byte)1);
        (await secondValues.FirstAsync(Token).AsTask().WithWatchdog()).ShouldBe((byte)2);
        host.Peripheral.ConnectedCentrals.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ASlowCentral_DoesNotDelayTheOthers_AndNotifyAllAsyncWaitsForEveryone()
    {
        var driver = new TestPeripheralDriver();
        await using BleAdapter adapter = await OpenTestAsync(driver);
        TypedLocalCharacteristic<byte, Properties.Notify> level = await AddLevelServiceAsync(adapter);
        TestCentralConnection slow = driver.ConnectSubscribed(1);
        driver.ConnectSubscribed(2);
        TaskCompletionSource release = driver.Hold(slow);

        ValueTask notifying = level.NotifyAllAsync((byte)5, Token);

        await Watchdog.UntilAsync(() => driver.Accepted.Count == 1);
        notifying.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await notifying.AsTask().WithWatchdog();
        driver.Accepted.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AFailingCentral_IsSkipped_AndTheCallersCancellationIsNot()
    {
        var driver = new TestPeripheralDriver();
        await using BleAdapter adapter = await OpenTestAsync(driver);
        TypedLocalCharacteristic<byte, Properties.Notify> level = await AddLevelServiceAsync(adapter);
        TestCentralConnection failing = driver.ConnectSubscribed(1);
        TestCentralConnection held = driver.ConnectSubscribed(2);
        driver.Failing.Add(failing);

        await level.NotifyAllAsync((byte)5, Token);
        driver.Accepted.ShouldHaveSingleItem();
        driver.Hold(held);
        using var cancellation = new CancellationTokenSource();
        ValueTask cancelled = level.NotifyAllAsync((byte)6, cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(cancelled.AsTask().WithWatchdog());
    }

    [Fact]
    public async Task NotifyAllAsync_SkipsACentralThatDisconnected_AndOneWhoseSubscriptionArrivedAfterItsDisconnect()
    {
        var driver = new TestPeripheralDriver();
        await using BleAdapter adapter = await OpenTestAsync(driver);
        TypedLocalCharacteristic<byte, Properties.Notify> level = await AddLevelServiceAsync(adapter);
        TestCentralConnection gone = driver.ConnectSubscribed(1);
        driver.ConnectSubscribed(2);
        driver.Sink.OnCentralDisconnected(gone, new DisconnectInfo(DisconnectReason.RemoteRequest));
        // A late callback must not bring the subscription back
        driver.Sink.OnSubscriptionChanged(gone, driver.AddedCharacteristic, enabled: true);

        await level.NotifyAllAsync((byte)5, Token);

        driver.Accepted.ShouldHaveSingleItem().Central.ShouldBe(new BleAddress(BleAddressType.Public, (UInt48)2));
    }

    [Fact]
    public async Task OpeningTheAdapter_AttachesThePeripheralBeforeItSetsTheIdentity()
    {
        var driver = new TestPeripheralDriver();

        await using BleAdapter adapter = await OpenTestAsync(driver, Capabilities.SetGapIdentity);

        driver.Calls.ShouldBe(["attach", "identity Test adapter"]);
    }

    private static async Task<TypedLocalCharacteristic<byte, Properties.Notify>> AddLevelServiceAsync(
        BleAdapter adapter
    )
    {
        var builder = new LocalGattServiceBuilder(Service);
        TypedLocalCharacteristic<byte, Properties.Notify> level = builder.AddCharacteristic(Level);
        await adapter.Peripheral.AddServiceAsync(builder, Token);
        return level;
    }

    private static async Task<TypedRemoteCharacteristic<byte, Properties.Notify>> DiscoverAsync(
        BleAdapter central,
        AdvertisingSet set
    )
    {
        ConnectedPeripheral connection = await central.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.DiscoverServiceAsync(Service, Token);
        return await service.DiscoverCharacteristicAsync(Level, Token);
    }

    private static async Task<BleAdapter> OpenTestAsync(
        TestPeripheralDriver driver,
        Capabilities features = Capabilities.None
    )
    {
        var manager = new BleManager(builder =>
            builder.AddBackend(
                new TestBackend(
                    new TestAdapterDriver
                    {
                        // A peripheral can always advertise too
                        Features = features,
                        Broadcaster = new TestBroadcasterDriver(),
                        Peripheral = driver,
                    }
                )
            )
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }

    private async Task<BleAdapter> OpenMockAsync(
        TaskCompletionSource<(
            BleAdapter,
            BleAdapter,
            AdvertisingSet,
            TypedLocalCharacteristic<byte, Properties.Notify>
        )> hosted
    )
    {
        var host =
            new TaskCompletionSource<(BleAdapter, AdvertisingSet, TypedLocalCharacteristic<byte, Properties.Notify>)>();
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options
                        .AddPeripheral(
                            async (peripheral, _) =>
                            {
                                TypedLocalCharacteristic<byte, Properties.Notify> level = await AddLevelServiceAsync(
                                    peripheral
                                );
                                AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                                    AdvertisingData.Empty,
                                    Interval
                                );
                                host.SetResult((peripheral, set, level));
                            },
                            "Host"
                        )
                        .AddPeripheral(
                            async (other, _) =>
                            {
                                (
                                    BleAdapter peripheral,
                                    AdvertisingSet set,
                                    TypedLocalCharacteristic<byte, Properties.Notify> level
                                ) = await host.Task;
                                hosted.SetResult((peripheral, other, set, level));
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
