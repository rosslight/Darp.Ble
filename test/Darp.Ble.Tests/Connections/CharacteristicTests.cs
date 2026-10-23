using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Reading and writing characteristics a mock peripheral hosts, through the public API </summary>
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
    public async Task WriteWithoutResponseAsync_ReachesTheWriteHandler_WithoutWaitingForIt()
    {
        var written = new TaskCompletionSource<byte[]>();
        var release = new TaskCompletionSource<GattProtocolStatus>();
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.WriteWithoutResponse,
                onWrite: async (_, value, _) =>
                {
                    written.SetResult(value.ToArray());
                    return await release.Task;
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        await level.WriteWithoutResponseAsync(new byte[] { 4 }, Token);

        (await written.Task).ShouldBe([4]);
        release.SetResult(GattProtocolStatus.WriteRequestRejected);
    }

    [Fact]
    public async Task AWriteWithoutResponseLongerThanTheMaxWritePayload_IsRejected_WithoutReachingThePeripheral()
    {
        var written = new TaskCompletionSource<byte[]>();
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.WriteWithoutResponse,
                onWrite: (_, value, _) =>
                {
                    written.SetResult(value.ToArray());
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);
        int max = adapter.Central.Connections.Single().MaxWritePayloadLength;

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await level.WriteWithoutResponseAsync(new byte[max + 1], Token)
        );
        await level.WriteWithoutResponseAsync(new byte[max], Token);

        (await written.Task).Length.ShouldBe(max);
    }

    [Fact]
    public async Task AWriteHandlerThatBlocksBeforeItsFirstAwait_DoesNotHoldUpTheWriteWithoutResponse()
    {
        using var release = new ManualResetEventSlim();
        var handled = new TaskCompletionSource();
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.WriteWithoutResponse,
                onWrite: (_, _, _) =>
                {
                    // Blocks the thread it runs on, as a careless handler might
                    release.Wait(TimeSpan.FromSeconds(10));
                    handled.SetResult();
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);

        await level
            .WriteWithoutResponseAsync(new byte[] { 4 }, Token)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), Token);

        handled.Task.IsCompleted.ShouldBeFalse();
        release.Set();
        await handled.Task;
    }

    [Fact]
    public async Task EndingTheConnection_FailsAReadWhoseHandlerIgnoresItsToken()
    {
        var release = new TaskCompletionSource<byte[]>();
        var started = new TaskCompletionSource();
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Read,
                onRead: (_, _) =>
                {
                    started.SetResult();
                    return new ValueTask<byte[]>(release.Task);
                }
            )
        );
        RemoteGattCharacteristic level = await DiscoverAsync(adapter, LevelUuid);
        Task<byte[]> read = level.ReadAsync(Token);
        await started.Task;

        await adapter.Central.Connections.Single().DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token);

        await Should.ThrowAsync<BleDisconnectedException>(() => read);
        release.SetResult([1]);
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

    [Fact]
    public async Task ATypedCharacteristic_EncodesWritesAndDecodesReads_ThroughItsDeclaration()
    {
        var stored = new byte[] { 0 };
        TypedCharacteristicDeclaration<byte, Properties.Read, Properties.Write> declaration =
            CharacteristicDeclaration.Create<byte, Properties.Read, Properties.Write>(LevelUuid);
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                LevelUuid,
                GattProperty.Read | GattProperty.Write,
                onRead: (_, _) => ValueTask.FromResult(stored),
                onWrite: (_, value, _) =>
                {
                    stored = value.ToArray();
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            )
        );
        RemoteGattService service = await DiscoverServiceAsync(adapter);

        TypedRemoteCharacteristic<byte, Properties.Read, Properties.Write> level =
            await service.DiscoverCharacteristicAsync(declaration, Token);
        await level.WriteAsync((byte)42, Token);

        (await level.ReadAsync(Token)).ShouldBe((byte)42);
        (await level.Characteristic.ReadAsync(Token)).ShouldBe([42]);
    }

    [Fact]
    public async Task ATypedRead_OfAValueTheDeclarationCannotDecode_FailsAsInvalidValue_NotAsAnAttError()
    {
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(LevelUuid, GattProperty.Read, onRead: (_, _) => ValueTask.FromResult<byte[]>([1]))
        );
        RemoteGattService service = await DiscoverServiceAsync(adapter);
        TypedRemoteCharacteristic<ushort, Properties.Read> level = await service.DiscoverCharacteristicAsync(
            CharacteristicDeclaration.Create<ushort, Properties.Read>(LevelUuid),
            Token
        );

        GattException exception = await Should.ThrowAsync<GattException>(() => level.ReadAsync(Token));

        exception.Kind.ShouldBe(GattErrorKind.InvalidValue);
        exception.AttStatus.ShouldBeNull();
    }

    [Fact]
    public async Task DiscoverCharacteristicAsync_WithADeclarationTheCharacteristicDoesNotMatch_FailsAsPropertyMismatch()
    {
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(LevelUuid, GattProperty.Read, onRead: (_, _) => ValueTask.FromResult<byte[]>([1]))
        );
        RemoteGattService service = await DiscoverServiceAsync(adapter);

        GattException mismatch = await Should.ThrowAsync<GattException>(() =>
            service.DiscoverCharacteristicAsync(new CharacteristicDeclaration<Properties.Write>(LevelUuid), Token)
        );
        GattException missing = await Should.ThrowAsync<GattException>(() =>
            service.DiscoverCharacteristicAsync(
                new CharacteristicDeclaration<Properties.Read>(BleUuid.FromUInt16(0x2A1A)),
                Token
            )
        );
        RemoteCharacteristic<Properties.Read> level = await service.DiscoverCharacteristicAsync(
            new CharacteristicDeclaration<Properties.Read>(LevelUuid),
            Token
        );

        mismatch.Kind.ShouldBe(GattErrorKind.PropertyMismatch);
        missing.Kind.ShouldBe(GattErrorKind.NotFound);
        (await level.ReadAsync(Token)).ShouldBe([1]);
    }

    private async Task<RemoteGattService> DiscoverServiceAsync(BleAdapter adapter)
    {
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            await _peripheralAddress.Task,
            cancellationToken: Token
        );
        return await connection.DiscoverServiceAsync(ServiceUuid, Token);
    }

    private async Task<RemoteGattCharacteristic> DiscoverAsync(BleAdapter adapter, BleUuid uuid)
    {
        RemoteGattService service = await DiscoverServiceAsync(adapter);
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
