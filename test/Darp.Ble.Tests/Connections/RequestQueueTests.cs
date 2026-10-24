using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Testing;
using Darp.Ble.Tests.Lifecycle;
using Darp.Ble.Tests.Scanning;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> How a connection's requests reach the driver, end and time out, through the public calls </summary>
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

        Task<IReadOnlyList<RemoteGattService>> first = connection.DiscoverServicesAsync(Token);
        Task<IReadOnlyList<RemoteGattService>> second = connection.DiscoverServicesAsync(Token);
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
        Task<IReadOnlyList<RemoteGattService>> running = connection.DiscoverServicesAsync(Token);
        PendingRequest runningRequest = await driver.NextRequestAsync(Token);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<RemoteGattService>> queued = connection.DiscoverServicesAsync(cancellation.Token);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => queued);
        runningRequest.Complete();
        await running;
        Task<IReadOnlyList<RemoteGattService>> next = connection.DiscoverServicesAsync(Token);
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
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(cancellation.Token);
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
            connection.DiscoverServicesAsync(new CancellationToken(canceled: true))
        );

        driver.HandedOverCount.ShouldBe(0);
    }

    [Fact]
    public async Task CancellingAfterTheRequestIsSent_AbandonsIt_AndTheNextRequestWaitsForItsAnswer()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<RemoteGattService>> abandoned = connection.DiscoverServicesAsync(cancellation.Token);
        PendingRequest sent = await driver.NextRequestAsync(Token);
        sent.Request.OnTransactionSent();

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => abandoned);
        sent.Request.IsAbandoned.ShouldBeTrue();
        sent.Request.HandOffToken.IsCancellationRequested.ShouldBeFalse();
        Task<IReadOnlyList<RemoteGattService>> next = connection.DiscoverServicesAsync(Token);
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
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(Token);
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
        Task<IReadOnlyList<RemoteGattService>> running = connection.DiscoverServicesAsync(Token);
        await driver.NextRequestAsync(Token);
        Task<IReadOnlyList<RemoteGattService>> queued = connection.DiscoverServicesAsync(Token);

        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.TransportLost, NativeCode: 0x08));
        await connection.DisposeAsync();
        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));

        var expected = new DisconnectInfo(DisconnectReason.TransportLost, NativeCode: 0x08);
        (await connection.Disconnected).ShouldBe(expected);
        (await Should.ThrowAsync<BleDisconnectedException>(() => running)).Info.ShouldBe(expected);
        (await Should.ThrowAsync<BleDisconnectedException>(() => queued)).Info.ShouldBe(expected);
        (
            await Should.ThrowAsync<BleDisconnectedException>(() => connection.DiscoverServicesAsync(Token))
        ).Info.ShouldBe(expected);
        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task ADriverThatLearnsOfTheLostLinkFromAFailedRequest_ReportsItFirst_AndTheCallerGetsADisconnect()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(Token);
        PendingRequest request = await driver.NextRequestAsync(Token);
        var info = new DisconnectInfo(DisconnectReason.ConnectionTimeout);
        var failure = new BleException("The device cannot be reached");

        driver.Sink.OnDisconnected(info);
        request.Fail(failure);

        BleDisconnectedException exception = await Should.ThrowAsync<BleDisconnectedException>(() => discovery);
        exception.Info.ShouldBe(info);
        exception.InnerException.ShouldBe(failure);
        (await connection.Disconnected).ShouldBe(info);
    }

    [Fact]
    public async Task ARequestThatFailsBecauseTheLinkWasLost_SeesTheConnectionEndedAndGone()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        ConnectedPeripheral connection = adapter.Central.Connections.Single();
        Task<(bool Disconnected, IReadOnlyList<ConnectedPeripheral> Open)> failed = ReadUntilDisconnectedAsync();
        await driver.NextRequestAsync(Token);
        // The driver's cleanup still runs when the read fails
        driver.HoldDispose = new TaskCompletionSource();

        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.ConnectionTimeout));

        (bool disconnected, IReadOnlyList<ConnectedPeripheral> open) = await ReleasedAfter(failed, driver);
        disconnected.ShouldBeTrue();
        open.ShouldBeEmpty();

        async Task<(bool, IReadOnlyList<ConnectedPeripheral>)> ReadUntilDisconnectedAsync()
        {
            try
            {
                await characteristic.ReadAsync(Token);
            }
            catch (BleDisconnectedException)
            {
                return (connection.Disconnected.IsCompleted, adapter.Central.Connections);
            }
            throw new InvalidOperationException("The read did not fail");
        }
    }

    [Fact]
    public async Task AStreamThatEndsBecauseTheLinkWasLost_SeesTheConnectionEndedAndGone()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        ConnectedPeripheral connection = adapter.Central.Connections.Single();
        using NotificationStream<byte[]> stream = characteristic.Subscribe();
        Task enable = characteristic.EnableNotificationsAsync(Token);
        PendingRequest cccd = await driver.NextRequestAsync(Token);
        cccd.Send();
        cccd.Answer();
        await enable;
        Task<(bool Disconnected, IReadOnlyList<ConnectedPeripheral> Open)> ended = EnumerateUntilDisconnectedAsync();
        // The driver's cleanup still runs when the stream ends
        driver.HoldDispose = new TaskCompletionSource();

        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.ConnectionTimeout));

        (bool disconnected, IReadOnlyList<ConnectedPeripheral> open) = await ReleasedAfter(ended, driver);
        disconnected.ShouldBeTrue();
        open.ShouldBeEmpty();

        async Task<(bool, IReadOnlyList<ConnectedPeripheral>)> EnumerateUntilDisconnectedAsync()
        {
            try
            {
                await foreach (byte[] _ in stream.WithCancellation(Token)) { }
            }
            catch (BleDisconnectedException)
            {
                return (connection.Disconnected.IsCompleted, adapter.Central.Connections);
            }
            throw new InvalidOperationException("The stream did not fail");
        }
    }

    [Fact]
    public async Task AfterALostLink_DisposingTheConnectionAndItsAdapter_WaitForTheDriversCleanup()
    {
        BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        driver.HoldDispose = new TaskCompletionSource();
        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.ConnectionTimeout));

        Task disposingConnection = connection.DisposeAsync().AsTask();
        Task disposingAdapter = adapter.DisposeAsync().AsTask();

        // Taken while the driver's cleanup still runs; asserted once it ended, so a failure cannot hang the test
        bool connectionWaited = !disposingConnection.IsCompleted;
        bool adapterWaited = !disposingAdapter.IsCompleted;
        driver.HoldDispose.SetResult();
        await disposingConnection.WithWatchdog();
        await disposingAdapter.WithWatchdog();
        connectionWaited.ShouldBeTrue();
        adapterWaited.ShouldBeTrue();
        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task AnAttErrorDuringDiscovery_ReachesTheCaller_AndTheConnectionStays()
    {
        await using BleAdapter adapter = await OpenAsync();
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(Token);

        (await driver.NextRequestAsync(Token)).Fail(
            new GattException(GattProtocolStatus.InsufficientAuthentication, "Authentication required")
        );

        GattException exception = await Should.ThrowAsync<GattException>(() => discovery);
        exception.Kind.ShouldBe(GattErrorKind.AttError);
        exception.AttStatus.ShouldBe(GattProtocolStatus.InsufficientAuthentication);
        connection.Disconnected.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task AWriteWithoutResponse_BehindASentRequest_GoesOutBeforeTheRequestIsAnswered()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        Task<byte[]> read = characteristic.ReadAsync(Token);
        PendingRequest sentRead = await driver.NextRequestAsync(Token);
        sentRead.Send();

        ValueTask write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);
        PendingRequest command = await driver.NextRequestAsync(Token);

        command.Kind.ShouldBe("command");
        command.Accept();
        await write;
        sentRead.Answer(new byte[] { 5 });
        (await read).ShouldBe([5]);
    }

    [Fact]
    public async Task AWriteWithoutResponse_NeverOvertakesAnEarlierRequestThatWaitsForTheAttSlot()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        Task<byte[]> first = characteristic.ReadAsync(Token);
        PendingRequest firstRead = await driver.NextRequestAsync(Token);
        firstRead.Send();
        Task<byte[]> second = characteristic.ReadAsync(Token);
        ValueTask write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);

        firstRead.Answer(new byte[] { 1 });
        await first;
        PendingRequest secondRead = await driver.NextRequestAsync(Token);
        secondRead.Kind.ShouldBe("read");
        secondRead.Send();
        PendingRequest command = await driver.NextRequestAsync(Token);

        command.Kind.ShouldBe("command");
        command.Accept();
        await write;
        secondRead.Answer(new byte[] { 2 });
        (await second).ShouldBe([2]);
    }

    [Fact]
    public async Task CancellingARequestThatWaitsForTheAttSlot_LetsTheWriteWithoutResponseBehindItGo()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        Task<byte[]> sent = characteristic.ReadAsync(Token);
        PendingRequest sentRead = await driver.NextRequestAsync(Token);
        sentRead.Send();
        using var cancellation = new CancellationTokenSource();
        Task<byte[]> waiting = characteristic.ReadAsync(cancellation.Token);
        ValueTask write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
        PendingRequest command = await driver.NextRequestAsync(Token);
        command.Kind.ShouldBe("command");
        command.Accept();
        await write;
        sentRead.Answer(new byte[] { 1 });
        await sent;
    }

    [Fact]
    public async Task CancellingAWriteWithoutResponse_WhileItWaitsForBufferSpace_EndsIt_AndTheNextOperationGoes()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        using var cancellation = new CancellationTokenSource();
        ValueTask write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, cancellation.Token);
        (await driver.NextRequestAsync(Token)).Kind.ShouldBe("command");

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => write.AsTask());
        Task<byte[]> read = characteristic.ReadAsync(Token);
        PendingRequest next = await driver.NextRequestAsync(Token);
        next.Kind.ShouldBe("read");
        next.Send();
        next.Answer(new byte[] { 3 });
        (await read).ShouldBe([3]);
    }

    [Fact]
    public async Task AWriteWithoutResponse_LeavesTheAttTimeoutOfASentRequestRunning()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        ConnectedPeripheral connection = adapter.Central.Connections.Single();
        Task<byte[]> read = characteristic.ReadAsync(Token);
        (await driver.NextRequestAsync(Token)).Send();
        _time.Advance(TimeSpan.FromSeconds(20));

        ValueTask write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);
        (await driver.NextRequestAsync(Token)).Accept();
        await write;
        _time.Advance(TimeSpan.FromSeconds(10));

        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.AttTimeout);
        await Should.ThrowAsync<BleDisconnectedException>(() => read);
    }

    [Fact]
    public async Task ALostLink_FailsTheSentRequest_TheWriteWithoutResponse_AndTheQueuedRequest()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        Task<byte[]> sent = characteristic.ReadAsync(Token);
        (await driver.NextRequestAsync(Token)).Send();
        Task write = characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token).AsTask();
        (await driver.NextRequestAsync(Token)).Kind.ShouldBe("command");
        Task<byte[]> queued = characteristic.ReadAsync(Token);

        driver.Sink.OnDisconnected(new DisconnectInfo(DisconnectReason.TransportLost));

        foreach (Task operation in new Task[] { sent, write, queued })
        {
            BleDisconnectedException exception = await Should.ThrowAsync<BleDisconnectedException>(() => operation);
            exception.Info.Reason.ShouldBe(DisconnectReason.TransportLost);
        }
    }

    [Fact]
    public async Task AnEnableTheCallerGaveUpOn_StillTurnsNotificationsOn_WhenThePeerAccepts()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        using NotificationStream<byte[]> stream = characteristic.Subscribe();
        using var cancellation = new CancellationTokenSource();
        Task enable = characteristic.EnableNotificationsAsync(cancellation.Token);
        PendingRequest cccd = await driver.NextRequestAsync(Token);
        cccd.Send();

        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => enable);
        cccd.Answer();
        // The next request only goes out once the CCCD write ended
        Task<byte[]> read = characteristic.ReadAsync(Token);
        (await driver.NextRequestAsync(Token)).Answer(new byte[] { 0 });
        await read;

        driver.Sink.OnNotification(driver.Characteristic, [9]);
        (await stream.FirstAsync(Token)).ShouldBe([9]);
    }

    [Fact]
    public async Task ANotificationWhileEnabling_ReachesTheStreams()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic characteristic, TestConnectionDriver driver) = await DiscoverAsync(adapter);
        using NotificationStream<byte[]> stream = characteristic.Subscribe();
        Task enable = characteristic.EnableNotificationsAsync(Token);
        PendingRequest cccd = await driver.NextRequestAsync(Token);
        cccd.Send();

        // Some devices send a value right after the CCCD write, before its answer
        driver.Sink.OnNotification(driver.Characteristic, [7]);
        cccd.Answer();
        await enable;

        (await stream.FirstAsync(Token)).ShouldBe([7]);
    }

    /// <summary> Waits for what the app saw while the driver's disposal is held, then lets the disposal go, also on a failure </summary>
    private static async Task<T> ReleasedAfter<T>(Task<T> seen, TestConnectionDriver driver)
    {
        try
        {
            return await seen.WithWatchdog();
        }
        finally
        {
            driver.HoldDispose?.TrySetResult();
        }
    }

    private async Task<(RemoteGattCharacteristic Characteristic, TestConnectionDriver Driver)> DiscoverAsync(
        BleAdapter adapter
    )
    {
        (ConnectedPeripheral connection, TestConnectionDriver driver) = await ConnectAsync(adapter);
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(Token);
        (await driver.NextRequestAsync(Token)).Complete(ServiceUuid);
        RemoteGattService service = (await discovery).Single();
        return ((await service.DiscoverCharacteristicsAsync(Token)).Single(), driver);
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
