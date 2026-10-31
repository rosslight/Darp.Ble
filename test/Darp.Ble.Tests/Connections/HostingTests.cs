using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Typed and marker hosted characteristics and hosted descriptors through the Mock </summary>
public sealed class HostingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly ServiceDeclaration Service = new(0xFFF0);

    private static readonly TypedCharacteristicDeclaration<ushort, Properties.Read, Properties.Write> Setpoint =
        CharacteristicDeclaration.Create<ushort, Properties.Read, Properties.Write>(0xFFF1);

    private static readonly TypedCharacteristicDeclaration<byte, Properties.Notify> Level =
        CharacteristicDeclaration.Create<byte, Properties.Notify>(0xFFF2);

    private static readonly CharacteristicDeclaration<Properties.Write> Control = new(0xFFF3);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set)> _host = new();

    [Fact]
    public async Task ATypedHostedCharacteristic_DecodesWritesAndEncodesReads_ThroughItsDeclaration()
    {
        ushort stored = 20;
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                Setpoint,
                onWrite: (_, value, _) =>
                {
                    stored = value;
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                },
                onRead: (_, _) => ValueTask.FromResult(stored)
            )
        );
        RemoteGattService service = await ConnectAsync(adapter);
        TypedRemoteCharacteristic<ushort, Properties.Read, Properties.Write> setpoint =
            await service.DiscoverCharacteristicAsync(Setpoint, Token);

        await setpoint.WriteAsync((ushort)500, Token);

        stored.ShouldBe((ushort)500);
        (await setpoint.ReadAsync(Token)).ShouldBe((ushort)500);
    }

    [Fact]
    public async Task AWriteWithTheWrongLength_IsAnsweredWithInvalidAttributeValueLength()
    {
        var calls = 0;
        await using BleAdapter adapter = await OpenAsync(service =>
            service.AddCharacteristic(
                Setpoint,
                onWrite: (_, _, _) =>
                {
                    calls++;
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            )
        );
        RemoteGattService service = await ConnectAsync(adapter);
        RemoteGattCharacteristic setpoint = (await service.DiscoverCharacteristicsAsync(Token)).Single();

        GattException exception = await Should.ThrowAsync<GattException>(() =>
            setpoint.WriteAsync(new byte[] { 1, 2, 3 }, Token)
        );

        exception.AttStatus.ShouldBe(GattProtocolStatus.InvalidAttributeValueLength);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task AMarkerAndATypedNotifyingCharacteristic_ReachTheirCentral()
    {
        var hosted =
            new TaskCompletionSource<(
                LocalCharacteristic<Properties.Write> Control,
                TypedLocalCharacteristic<byte, Properties.Notify> Level
            )>();
        var written = new TaskCompletionSource<byte[]>();
        await using BleAdapter adapter = await OpenAsync(service =>
            hosted.SetResult(
                (
                    service.AddCharacteristic(
                        Control,
                        onWrite: (_, value, _) =>
                        {
                            written.SetResult(value.ToArray());
                            return ValueTask.FromResult(GattProtocolStatus.Success);
                        }
                    ),
                    service.AddCharacteristic(Level)
                )
            )
        );
        RemoteGattService service = await ConnectAsync(adapter);
        RemoteCharacteristic<Properties.Write> control = await service.DiscoverCharacteristicAsync(Control, Token);
        TypedRemoteCharacteristic<byte, Properties.Notify> level = await service.DiscoverCharacteristicAsync(
            Level,
            Token
        );
        using NotificationStream<byte> levels = level.Subscribe();
        await level.EnableNotificationsAsync(Token);
        (_, TypedLocalCharacteristic<byte, Properties.Notify> localLevel) = await hosted.Task;
        ConnectedCentral central = (await _host.Task).Host.Peripheral.ConnectedCentrals.Single();

        await control.WriteAsync(new byte[] { 9 }, Token);
        await localLevel.NotifyAsync(central, (byte)77, Token);

        (await written.Task).ShouldBe([9]);
        (await levels.FirstAsync(Token)).ShouldBe((byte)77);
    }

    [Fact]
    public async Task HostedDescriptors_AnswerThroughTheirOwnHandlers_AndAUserDescriptionIsReadOnly()
    {
        byte[] stored = [0];
        await using BleAdapter adapter = await OpenAsync(service =>
        {
            LocalGattCharacteristic level = service.AddCharacteristic(
                0xFFF4,
                GattProperty.Read,
                onRead: (_, _) => ValueTask.FromResult<byte[]>([50])
            );
            level.AddUserDescription("main");
            level.AddDescriptor(
                0xFFFA,
                onRead: (_, _) => ValueTask.FromResult(stored),
                onWrite: (_, value, _) =>
                {
                    stored = value.ToArray();
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            );
        });
        RemoteGattService service = await ConnectAsync(adapter);
        RemoteGattCharacteristic level = (await service.DiscoverCharacteristicsAsync(Token)).Single();

        IReadOnlyList<RemoteDescriptor> descriptors = await level.DiscoverDescriptorsAsync(Token);
        RemoteDescriptor description = descriptors.Single(d => d.Uuid == BleUuid.FromUInt16(0x2901));
        RemoteDescriptor custom = descriptors.Single(d => d.Uuid == BleUuid.FromUInt16(0xFFFA));
        await custom.WriteAsync(new byte[] { 7 }, Token);

        byte[] firstRead = await description.ReadAsync(Token);
        // Changing what a read returned changes nothing on the peripheral
        firstRead[0] = (byte)'X';
        (await description.ReadAsync(Token)).ShouldBe("main"u8.ToArray());
        (await custom.ReadAsync(Token)).ShouldBe([7]);
        (
            await Should.ThrowAsync<GattException>(() => description.WriteAsync(new byte[] { 1 }, Token))
        ).AttStatus.ShouldBe(GattProtocolStatus.WriteNotPermitted);
    }

    [Fact]
    public async Task TwoCharacteristicsWithTheSameUuid_EachAnswerThroughTheirOwnHandler()
    {
        await using BleAdapter adapter = await OpenAsync(service =>
        {
            service.AddCharacteristic(0xFFF5, GattProperty.Read, onRead: (_, _) => ValueTask.FromResult<byte[]>([1]));
            service.AddCharacteristic(0xFFF5, GattProperty.Read, onRead: (_, _) => ValueTask.FromResult<byte[]>([2]));
        });
        RemoteGattService service = await ConnectAsync(adapter);

        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);

        (await characteristics[0].ReadAsync(Token)).ShouldBe([1]);
        (await characteristics[1].ReadAsync(Token)).ShouldBe([2]);
    }

    [Fact]
    public async Task ABuilder_CannotChangeOrBeAddedAgain_OnceAdded()
    {
        var builder = new LocalGattServiceBuilder(Service);
        LocalGattCharacteristic characteristic = builder.AddCharacteristic(0xFFF6, GattProperty.Read);
        await using BleAdapter adapter = await OpenAsync(_ => { });

        await adapter.Peripheral.AddServiceAsync(builder, Token);

        Should.Throw<InvalidOperationException>(() => builder.AddCharacteristic(0xFFF7, GattProperty.Read));
        Should.Throw<InvalidOperationException>(() => characteristic.AddUserDescription("late"));
        await Should.ThrowAsync<InvalidOperationException>(() => adapter.Peripheral.AddServiceAsync(builder, Token));
    }

    private async Task<RemoteGattService> ConnectAsync(BleAdapter adapter)
    {
        (_, AdvertisingSet set) = await _host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        return await connection.DiscoverServiceAsync(Service, Token);
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
                            var service = new LocalGattServiceBuilder(Service);
                            host(service);
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
}
