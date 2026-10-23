using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Tests.Lifecycle;
using Darp.Ble.Tests.Scanning;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> How a connection's requests reach the driver, end and time out, through the public calls (seam 3) </summary>
public sealed class RequestQueueTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180D);

    private readonly FakeTimeProvider _time = new();
    private readonly TestCentralDriver _central = new();

    [Fact]
    public async Task Requests_ReachTheDriverOneAtATime_InCallOrder()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);

        Task<IReadOnlyList<RemoteGattService>> first = connection.Gatt.DiscoverServicesAsync(Token);
        Task<IReadOnlyList<RemoteGattService>> second = connection.Gatt.DiscoverServicesAsync(Token);
        PendingRequest firstRequest = await driver.NextRequestAsync(Token);

        driver.HandedOverCount.ShouldBe(1);
        firstRequest.Complete(ServiceUuid);
        (await first).ShouldHaveSingleItem().Uuid.ShouldBe(ServiceUuid);
        (await driver.NextRequestAsync(Token)).Complete();
        (await second).ShouldBeEmpty();
    }

    [Fact]
    public async Task CancellingAQueuedRequest_DropsIt_BeforeItReachesTheDriver()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> running = connection.Gatt.DiscoverServicesAsync(Token);
        PendingRequest runningRequest = await driver.NextRequestAsync(Token);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<RemoteGattService>> queued = connection.Gatt.DiscoverServicesAsync(cancellation.Token);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => queued);
        runningRequest.Complete();
        await running;
        Task<IReadOnlyList<RemoteGattService>> next = connection.Gatt.DiscoverServicesAsync(Token);
        (await driver.NextRequestAsync(Token)).Complete(ServiceUuid);
        (await next).ShouldHaveSingleItem();
        driver.HandedOverCount.ShouldBe(2);
    }

    [Fact]
    public async Task CancellingBeforeTheRequestIsSent_CancelsTheHandOff()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.Gatt.DiscoverServicesAsync(cancellation.Token);
        PendingRequest request = await driver.NextRequestAsync(Token);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => discovery);
        request.Request.HandOffToken.IsCancellationRequested.ShouldBeTrue();
        request.Request.IsAbandoned.ShouldBeFalse();
    }

    [Fact]
    public async Task ARequestCancelledBeforeItsTurn_NeverReachesTheDriver()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            connection.Gatt.DiscoverServicesAsync(new CancellationToken(canceled: true))
        );

        driver.HandedOverCount.ShouldBe(0);
    }

    [Fact]
    public async Task CancellingAfterTheRequestIsSent_AbandonsIt_AndTheNextRequestWaitsForItsAnswer()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<RemoteGattService>> abandoned = connection.Gatt.DiscoverServicesAsync(cancellation.Token);
        PendingRequest sent = await driver.NextRequestAsync(Token);
        sent.Request.OnTransactionSent();

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => abandoned);
        sent.Request.IsAbandoned.ShouldBeTrue();
        sent.Request.HandOffToken.IsCancellationRequested.ShouldBeFalse();
        Task<IReadOnlyList<RemoteGattService>> next = connection.Gatt.DiscoverServicesAsync(Token);
        driver.HandedOverCount.ShouldBe(1);
        sent.Complete();
        (await driver.NextRequestAsync(Token)).Complete(ServiceUuid);
        (await next).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task APeerThatStopsAnswering_EndsTheConnectionAfterTheAttTimeoutOfItsTransaction()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.Gatt.DiscoverServicesAsync(Token);
        PendingRequest request = await driver.NextRequestAsync(Token);

        // Each transaction gets its own 30 s: the first page is answered in time, the second one never
        request.Request.OnTransactionSent();
        _time.Advance(TimeSpan.FromSeconds(29));
        request.Request.OnTransactionAnswered();
        request.Request.OnTransactionSent();
        _time.Advance(TimeSpan.FromSeconds(29));
        connection.Disconnected.IsCompleted.ShouldBeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));

        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.AttTimeout);
        BleDisconnectedException exception = await Should.ThrowAsync<BleDisconnectedException>(() => discovery);
        exception.Info.Reason.ShouldBe(DisconnectReason.AttTimeout);
        driver.DisposeCount.ShouldBe(1);
        adapter.Central.Connections.ShouldBeEmpty();
    }

    [Fact]
    public async Task ALostLink_FailsRunningAndQueuedRequests_WithTheFirstReasonOnly()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> running = connection.Gatt.DiscoverServicesAsync(Token);
        await driver.NextRequestAsync(Token);
        Task<IReadOnlyList<RemoteGattService>> queued = connection.Gatt.DiscoverServicesAsync(Token);

        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.TransportLost, NativeCode: 0x08));
        await connection.DisposeAsync();
        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));

        var expected = new DisconnectInfo(DisconnectReason.TransportLost, NativeCode: 0x08);
        (await connection.Disconnected).ShouldBe(expected);
        (await Should.ThrowAsync<BleDisconnectedException>(() => running)).Info.ShouldBe(expected);
        (await Should.ThrowAsync<BleDisconnectedException>(() => queued)).Info.ShouldBe(expected);
        (
            await Should.ThrowAsync<BleDisconnectedException>(() => connection.Gatt.DiscoverServicesAsync(Token))
        ).Info.ShouldBe(expected);
        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task AnAttErrorDuringDiscovery_ReachesTheCaller_AndTheConnectionStays()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.Gatt.DiscoverServicesAsync(Token);

        (await driver.NextRequestAsync(Token)).Fail(
            new GattException(GattProtocolStatus.InsufficientAuthentication, "Authentication required")
        );

        GattException exception = await Should.ThrowAsync<GattException>(() => discovery);
        exception.Kind.ShouldBe(GattErrorKind.AttError);
        exception.AttStatus.ShouldBe(GattProtocolStatus.InsufficientAuthentication);
        connection.Disconnected.IsCompleted.ShouldBeFalse();
    }

    private async Task<(ConnectedPeripheral Connection, TestConnectionDriver Driver)> ConnectAsync(BleAdapter adapter)
    {
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            BleAddress.NewRandomStaticAddress(),
            cancellationToken: Token
        );
        return (connection, await _central.Connection);
    }

    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(
            builder =>
                builder.AddBackend(
                    new TestBackend(
                        new TestAdapterDriver
                        {
                            // A central can always scan too
                            Capabilities = Capabilities.Central,
                            Observer = new TestObserverDriver(),
                            Central = _central,
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
