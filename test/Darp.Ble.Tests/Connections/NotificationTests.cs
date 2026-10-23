using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Notifications from a mock peripheral through the public API (seam 2, D7) </summary>
public sealed class NotificationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180D);
    private static readonly BleUuid MeasurementUuid = BleUuid.FromUInt16(0x2A37);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)> _peripheral = new();
    private LocalGattCharacteristic? _measurement;

    [Fact]
    public async Task AStreamSubscribedBeforeEnabling_GetsWhatThePeripheralNotifiesAfterwards()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = measurement.Subscribe();

        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 1);

        (await stream.FirstAsync(Token)).ShouldBe([1]);
    }

    [Fact]
    public async Task EnumeratingAStream_WithoutEnablingNotifications_Throws()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, _) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = measurement.Subscribe();

        await Should.ThrowAsync<InvalidOperationException>(() => stream.FirstAsync(Token).AsTask());
    }

    [Fact]
    public async Task TwoStreams_EachGetEveryNotification_AsTheirOwnCopy()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> first = measurement.Subscribe();
        using NotificationStream<byte[]> second = measurement.Subscribe();
        await measurement.EnableNotificationsAsync(Token);

        await NotifyAsync(central, 1);
        await NotifyAsync(central, 2);

        byte[][] fromFirst = await first.Take(2).ToArrayAsync(Token);
        fromFirst[0][0] = 99;
        (await second.Take(2).ToArrayAsync(Token)).ShouldBe([
            [1],
            [2],
        ]);
    }

    [Fact]
    public async Task EnumerationsOneAfterAnother_ContinueOnTheSameQueue_AndConcurrentOnesThrow()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = measurement.Subscribe();
        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 1);
        await NotifyAsync(central, 2);

        (await stream.FirstAsync(Token)).ShouldBe([1]);
        await using IAsyncEnumerator<byte[]> enumerator = stream.GetAsyncEnumerator(Token);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        enumerator.Current.ShouldBe([2]);

        await Should.ThrowAsync<InvalidOperationException>(() => stream.FirstAsync(Token).AsTask());
    }

    [Fact]
    public async Task DisablingNotifications_EndsTheStreamsAfterTheirQueuedValues_AndEnablingAgainDoesNotReviveThem()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = measurement.Subscribe();
        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 1);

        await measurement.DisableNotificationsAsync(Token);
        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 2);

        await using IAsyncEnumerator<byte[]> enumerator = stream.GetAsyncEnumerator(Token);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        enumerator.Current.ShouldBe([1]);
        await Should.ThrowAsync<InvalidOperationException>(() => enumerator.MoveNextAsync().AsTask());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamsEndWithTheirConnection_NormallyAfterALocalEnd_WithTheDisconnectionOtherwise(bool remoteEnd)
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> stream = measurement.Subscribe();
        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 1);

        if (remoteEnd)
            await central.DisconnectAsync(Token);
        else
            await adapter.Central.Connections.Single().DisposeAsync();

        await using IAsyncEnumerator<byte[]> enumerator = stream.GetAsyncEnumerator(Token);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        enumerator.Current.ShouldBe([1]);
        if (remoteEnd)
        {
            BleDisconnectedException exception = await Should.ThrowAsync<BleDisconnectedException>(() =>
                enumerator.MoveNextAsync().AsTask()
            );
            exception.Info.Reason.ShouldBe(DisconnectReason.RemoteRequest);
        }
        else
        {
            (await enumerator.MoveNextAsync()).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task AFullQueue_EndsOnlyThatStream_AfterItsQueuedValues()
    {
        await using BleAdapter adapter = await OpenAsync();
        (RemoteGattCharacteristic measurement, ConnectedCentral central) = await ConnectAsync(adapter);
        using NotificationStream<byte[]> small = measurement.Subscribe(new QueueOptions(1, QueueFullMode.Fail));
        using NotificationStream<byte[]> other = measurement.Subscribe();
        await measurement.EnableNotificationsAsync(Token);

        await NotifyAsync(central, 1);
        await NotifyAsync(central, 2);

        await using IAsyncEnumerator<byte[]> enumerator = small.GetAsyncEnumerator(Token);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        await Should.ThrowAsync<BleQueueOverflowException>(() => enumerator.MoveNextAsync().AsTask());
        (await other.Take(2).ToArrayAsync(Token)).ShouldBe([
            [1],
            [2],
        ]);
    }

    [Fact]
    public async Task ATypedStream_DecodesTheValues()
    {
        await using BleAdapter adapter = await OpenAsync();
        (_, ConnectedCentral central) = await ConnectAsync(adapter);
        RemoteGattService service = await adapter
            .Central.Connections.Single()
            .Gatt.DiscoverServiceAsync(ServiceUuid, Token);
        TypedRemoteCharacteristic<byte, Properties.Notify> measurement = await service.DiscoverCharacteristicAsync(
            CharacteristicDeclaration.Create<byte, Properties.Notify>(MeasurementUuid),
            Token
        );
        using NotificationStream<byte> stream = measurement.Subscribe();

        await measurement.EnableNotificationsAsync(Token);
        await NotifyAsync(central, 7);

        (await stream.FirstAsync(Token)).ShouldBe((byte)7);
    }

    private ValueTask NotifyAsync(ConnectedCentral central, byte value) =>
        (_measurement ?? throw new InvalidOperationException("Not hosted")).NotifyAsync(
            central,
            new[] { value },
            Token
        );

    private async Task<(RemoteGattCharacteristic Measurement, ConnectedCentral Central)> ConnectAsync(
        BleAdapter adapter
    )
    {
        (BleAdapter peripheral, AdvertisingSet set) = await _peripheral.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(ServiceUuid, Token);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);
        return (characteristics.Single(), peripheral.Peripheral.ConnectedCentrals.Single());
    }

    /// <summary> Opens the mock adapter with one peripheral that hosts a notifying characteristic and advertises </summary>
    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        var service = new LocalGattServiceBuilder(ServiceUuid);
                        _measurement = service.AddCharacteristic(MeasurementUuid, GattProperty.Notify);
                        await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                        AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                            AdvertisingData.Empty,
                            Interval
                        );
                        _peripheral.SetResult((peripheral, set));
                    })
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
