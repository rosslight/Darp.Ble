using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Darp.Ble.Tests.Advertising;
using Darp.Ble.Tests.Lifecycle;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Hosting and accepting connections on a mock peripheral through the public API </summary>
public sealed class PeripheralTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.Empty.WithCompleteLocalName("Sensor");

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task ACentralThatConnects_ShowsUpUntilItDisconnects()
    {
        var advertising = new TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(
                        (peripheral, await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                    )
            )
        );
        (BleAdapter peripheral, AdvertisingSet set) = await advertising.Task;

        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );

        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();
        central.Address.ShouldBe(adapter.RandomAddress);
        await connection.DisposeAsync();
        (await central.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
        peripheral.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisconnectingACentral_EndsTheConnectionOnBothSides()
    {
        var advertising = new TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(
                        (peripheral, await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                    )
            )
        );
        (BleAdapter peripheral, AdvertisingSet set) = await advertising.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();

        await central.DisconnectAsync(Token);

        (await central.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
        adapter.Central.Connections.ShouldBeEmpty();
        peripheral.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    [Fact]
    public async Task WatchCentralsAsync_YieldsACentralThatConnects_AlsoOneThatSendsNoRequest()
    {
        var advertising = new TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(
                        (peripheral, await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                    )
            )
        );
        (BleAdapter peripheral, AdvertisingSet set) = await advertising.Task;
        await using IAsyncEnumerator<ConnectedCentral> centrals = peripheral
            .Peripheral.WatchCentralsAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> next = centrals.MoveNextAsync();

        await adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: Token);

        (await next.AsTask().WithWatchdog()).ShouldBeTrue();
        centrals.Current.Address.ShouldBe(adapter.RandomAddress);
    }

    [Fact]
    public async Task WatchCentralsAsync_YieldsTheConnectedCentralsFirst_ThenEachNewOne_AlsoOneThatLeftAgain()
    {
        var driver = new TestPeripheralDriver();
        await using BleAdapter adapter = await OpenTestAsync(driver);
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(1)));
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(2)));
        await using IAsyncEnumerator<ConnectedCentral> centrals = adapter
            .Peripheral.WatchCentralsAsync(Token)
            .GetAsyncEnumerator(Token);
        (await centrals.MoveNextAsync()).ShouldBeTrue();
        ConnectedCentral first = centrals.Current;

        var left = new TestCentralConnection(Address(3));
        driver.Sink.OnCentralConnected(left);
        driver.Sink.OnCentralDisconnected(left, new DisconnectInfo(DisconnectReason.RemoteRequest));
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(4)));

        List<ConnectedCentral> yielded = [first];
        while (yielded.Count < 4 && await centrals.MoveNextAsync().AsTask().WithWatchdog())
            yielded.Add(centrals.Current);
        yielded.Select(central => central.Address).ShouldBe([Address(1), Address(2), Address(3), Address(4)]);
        (await yielded[2].Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.RemoteRequest);
    }

    [Fact]
    public async Task WatchCentralsAsync_YieldsTheConnectedCentralsInConnectOrder_AlsoAfterAnEarlierOneLeft()
    {
        var driver = new TestPeripheralDriver();
        await using BleAdapter adapter = await OpenTestAsync(driver);
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(1)));
        ConnectedCentral first = adapter.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(2)));
        // Gone for good, so the next central may take its place in the peripheral's books
        await first.ReleaseAsync().WithWatchdog();
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(3)));

        await using IAsyncEnumerator<ConnectedCentral> centrals = adapter
            .Peripheral.WatchCentralsAsync(Token)
            .GetAsyncEnumerator(Token);
        List<BleAddress> yielded = [];
        while (yielded.Count < 2 && await centrals.MoveNextAsync().AsTask().WithWatchdog())
            yielded.Add(centrals.Current.Address);

        yielded.ShouldBe([Address(2), Address(3)]);
        adapter.Peripheral.ConnectedCentrals.Select(central => central.Address).ShouldBe([Address(2), Address(3)]);
    }

    [Fact]
    public async Task WatchCentralsAsync_EndsWhenTheAdapterIsDisposed_AndCannotStartAfterwards()
    {
        var driver = new TestPeripheralDriver();
        BleAdapter adapter = await OpenTestAsync(driver);
        driver.Sink.OnCentralConnected(new TestCentralConnection(Address(1)));
        await using IAsyncEnumerator<ConnectedCentral> centrals = adapter
            .Peripheral.WatchCentralsAsync(Token)
            .GetAsyncEnumerator(Token);
        (await centrals.MoveNextAsync()).ShouldBeTrue();
        ValueTask<bool> next = centrals.MoveNextAsync();

        await adapter.DisposeAsync();

        (await next.AsTask().WithWatchdog()).ShouldBeFalse();
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
        {
            await foreach (ConnectedCentral _ in adapter.Peripheral.WatchCentralsAsync(Token)) { }
        });
    }

    private static BleAddress Address(byte value) => new(BleAddressType.Public, (UInt48)value);

    private static async Task<BleAdapter> OpenTestAsync(TestPeripheralDriver driver)
    {
        var manager = new BleManager(builder =>
            builder.AddBackend(
                new TestBackend(
                    new TestAdapterDriver { Broadcaster = new TestBroadcasterDriver(), Peripheral = driver }
                )
            )
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }

    private async Task<BleAdapter> OpenAsync(Action<MockBackendOptions> configure)
    {
        var manager = new BleManager(builder => builder.AddMock(configure), timeProvider: _time);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
