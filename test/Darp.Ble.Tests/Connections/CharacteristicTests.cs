using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Reading and writing characteristics a mock peripheral hosts, through the public API (seam 1) </summary>
public sealed class CharacteristicTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly BleUuid ServiceUuid = BleUuid.FromUInt16(0x180F);
    private static readonly BleUuid LevelUuid = BleUuid.FromUInt16(0x2A19);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<BleAddress> _peripheralAddress = new();

    [Fact]
    public async Task ReadAsync_ReturnsWhatThePeripheralsReadHandlerAnswers()
    {
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Read,
                onRead: (_, _) => ValueTask.FromResult<byte[]>([87])
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        byte[] value = await level.ReadAsync(Token);

        value.ShouldBe([87]);
    }

    [Fact]
    public async Task WriteAsync_HandsTheValueToThePeripheralsWriteHandler_WithTheWritingCentral()
    {
        var written = new TaskCompletionSource<(BleAddress Central, byte[] Value)>();
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Write,
                onWrite: (central, value, _) =>
                {
                    written.SetResult((central.Address, value.ToArray()));
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        await level.WriteAsync(new byte[] { 1, 2, 3 }, Token);

        (BleAddress central, byte[] value) = await written.Task;
        central.ShouldBe(adapter.RandomAddress);
        value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task AWriteHandlerAnsweringWithAnError_FailsTheWriteWithThatStatus()
    {
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Write,
                onWrite: (_, _, _) => ValueTask.FromResult(GattProtocolStatus.WriteRequestRejected)
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        GattException exception = await Should.ThrowAsync<GattException>(() =>
            level.WriteAsync(new byte[] { 1 }, Token)
        );

        exception.Kind.ShouldBe(GattErrorKind.AttError);
        exception.AttStatus.ShouldBe(GattProtocolStatus.WriteRequestRejected);
    }

    [Theory]
    [InlineData(true, GattProtocolStatus.InsufficientAuthentication)]
    [InlineData(false, GattProtocolStatus.UnlikelyError)]
    public async Task AReadHandlerThatThrows_AnswersWithItsAttStatus_OrUnlikelyError(
        bool throwsGattException,
        GattProtocolStatus expected
    )
    {
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Read,
                onRead: (_, _) =>
                    throwsGattException
                        ? throw new GattException(GattProtocolStatus.InsufficientAuthentication, "Pair first")
                        : throw new InvalidOperationException("Handler bug")
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        GattException exception = await Should.ThrowAsync<GattException>(() => level.ReadAsync(Token));

        exception.AttStatus.ShouldBe(expected);
    }

    [Fact]
    public async Task AnOperationTheCharacteristicDoesNotSupport_FailsAsPropertyMismatch_WithoutReachingThePeripheral()
    {
        var reads = 0;
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Notify,
                onRead: (_, _) =>
                {
                    reads++;
                    return ValueTask.FromResult<byte[]>([]);
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        GattException exception = await Should.ThrowAsync<GattException>(() => level.ReadAsync(Token));

        exception.Kind.ShouldBe(GattErrorKind.PropertyMismatch);
        reads.ShouldBe(0);
    }

    private async Task<RemoteGattCharacteristic> DiscoverAsync(BleAdapter adapter, BleUuid uuid)
    {
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            await _peripheralAddress.Task,
            cancellationToken: Token
        );
        RemoteGattService service = await connection.DiscoverServiceAsync(ServiceUuid, Token);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);
        return characteristics.Single(characteristic => characteristic.Uuid == uuid);
    }

    /// <summary> Opens the mock adapter with one peripheral that hosts the service and advertises </summary>
    private async Task<BleAdapter> OpenAsync(Action<LocalGattServiceBuilder> host)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            var service = new LocalGattServiceBuilder(ServiceUuid);
                            host(service);
                            await peripheral.Peripheral.AddServiceAsync(service);
                            AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                                AdvertisingData.Empty,
                                Interval
                            );
                            _peripheralAddress.SetResult(set.RandomAddress);
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
