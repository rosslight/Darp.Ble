using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Typed and marker hosted characteristics and hosted descriptors through the Mock (seam 1 of PR 4b) </summary>
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

    private async Task<RemoteGattService> ConnectAsync(BleAdapter adapter)
    {
        (_, AdvertisingSet set) = await _host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        return await connection.Gatt.DiscoverServiceAsync(Service, Token);
    }

    /// <summary> Opens the mock adapter with one peripheral that hosts the service and advertises </summary>
    private async Task<BleAdapter> OpenAsync(Action<LocalGattServiceBuilder> host)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        var service = new LocalGattServiceBuilder(Service);
                        host(service);
                        await peripheral.Peripheral.Gatt.AddServiceAsync(service);
                        _host.SetResult(
                            (
                                peripheral,
                                await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                            )
                        );
                    })
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
