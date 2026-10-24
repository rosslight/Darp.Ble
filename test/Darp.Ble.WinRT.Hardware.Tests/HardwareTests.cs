using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.HciHost;
using Shouldly;

namespace Darp.Ble.WinRT.Hardware.Tests;

/// <summary>
/// Windows' radio against the nRF52840 dongle on the same machine, which runs HciHost.
/// Each test opens both adapters, so they run one after another
/// </summary>
[Collection(nameof(HardwareTests))]
[CollectionDefinition(nameof(HardwareTests), DisableParallelization = true)]
public sealed class HardwareTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);
    private static readonly BleUuid ServiceUuid = BleUuid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", null);
    private static readonly BleUuid ValueUuid = BleUuid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E", null);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<BleAdapter> OpenAsync(Action<BleManagerBuilder> add)
    {
        IReadOnlyList<BleAdapterInfo> adapters = await new BleManager(add).EnumerateAdaptersAsync(Token);
        return await adapters.ShouldHaveSingleItem().OpenAsync(Token);
    }

    private static Task<BleAdapter> OpenWindowsAsync() => OpenAsync(builder => builder.AddWinRT());

    private static Task<BleAdapter> OpenDongleAsync() => OpenAsync(builder => builder.AddSerialHciHost());

    private static AdvertisingData Marker(byte id) =>
        AdvertisingData.From([(AdTypes.ManufacturerSpecificData, new byte[] { 0xFF, 0xFF, 0xDA, 0x7B, id })]);

    private static async Task<IGapAdvertisement> FindAsync(BleAdapter adapter, Func<IGapAdvertisement, bool> match)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(Patience);
        await foreach (IGapAdvertisement advertisement in adapter.Observer.ScanAsync(timeout.Token))
        {
            if (match(advertisement))
                return advertisement;
        }
        throw new TimeoutException("Nothing matching was heard");
    }

    private static bool HasMarker(IGapAdvertisement advertisement, byte id) =>
        advertisement.Data.TryGetFirstType(AdTypes.ManufacturerSpecificData, out ReadOnlyMemory<byte> bytes)
        && bytes.Span.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xDA, 0x7B, id });

    [Fact]
    public async Task TheDefaultAdapter_Opens_WithItsRoles()
    {
        await using BleAdapter windows = await OpenWindowsAsync();

        windows.Backend.ShouldBe(BleBackends.WinRT);
        windows.Capabilities.HasFlag(Capabilities.Observer).ShouldBeTrue();
        TestContext.Current.SendDiagnosticMessage($"Capabilities: {windows.Capabilities}");
    }

    [Fact]
    public async Task Windows_HearsTheDongleAdvertise()
    {
        await using BleAdapter windows = await OpenWindowsAsync();
        await using BleAdapter dongle = await OpenDongleAsync();
        await using AdvertisingSet set = await dongle.Broadcaster.StartAdvertisingAsync(
            Marker(1),
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvNonConnInd,
            cancellationToken: Token
        );

        IGapAdvertisement heard = await FindAsync(windows, advertisement => HasMarker(advertisement, 1));

        heard.Address.Value.ShouldBe(set.RandomAddress.Value);
    }

    [Fact]
    public async Task TheDongle_HearsWindowsAdvertise()
    {
        await using BleAdapter windows = await OpenWindowsAsync();
        await using BleAdapter dongle = await OpenDongleAsync();
        await using AdvertisingSet set = await windows.Broadcaster.StartAdvertisingAsync(
            Marker(2),
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvNonConnInd,
            cancellationToken: Token
        );

        await FindAsync(dongle, advertisement => HasMarker(advertisement, 2));
    }

    [Fact]
    public async Task Windows_UsesTheServicesTheDongleHosts()
    {
        await using BleAdapter windows = await OpenWindowsAsync();
        await using BleAdapter dongle = await OpenDongleAsync();
        byte[] stored = [42];
        var service = new LocalGattServiceBuilder(ServiceUuid);
        LocalGattCharacteristic value = service.AddCharacteristic(
            ValueUuid,
            GattProperty.Read | GattProperty.Write | GattProperty.Notify,
            onRead: (_, _) => ValueTask.FromResult(stored),
            onWrite: (_, written, _) =>
            {
                stored = written.ToArray();
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
        await dongle.Peripheral.AddServiceAsync(service, Token);
        await using AdvertisingSet set = await dongle.Broadcaster.StartAdvertisingAsync(
            Marker(3),
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvInd,
            cancellationToken: Token
        );
        IGapAdvertisement heard = await FindAsync(windows, advertisement => HasMarker(advertisement, 3));

        await using ConnectedPeripheral connection = await windows.Central.ConnectAsync(
            heard.Address,
            cancellationToken: Token
        );
        RemoteGattCharacteristic remote = (
            await (await connection.DiscoverServiceAsync(ServiceUuid, Token)).DiscoverCharacteristicsAsync(Token)
        ).Single(c => c.Uuid.Value == ValueUuid.Value);
        byte[] read = await remote.ReadAsync(Token);
        await remote.WriteAsync(new byte[] { 7 }, Token);
        using NotificationStream<byte[]> notifications = remote.Subscribe();
        await remote.EnableNotificationsAsync(Token);
        await value.NotifyAllAsync(new byte[] { 9 }, Token);

        read.ShouldBe([42]);
        stored.ShouldBe([7]);
        (await notifications.FirstAsync(Token).AsTask().WaitAsync(Patience, Token)).ShouldBe([9]);
    }

    [Fact]
    public async Task TheDongle_UsesTheServicesWindowsHosts()
    {
        await using BleAdapter windows = await OpenWindowsAsync();
        await using BleAdapter dongle = await OpenDongleAsync();
        byte[] stored = [42];
        var service = new LocalGattServiceBuilder(ServiceUuid);
        LocalGattCharacteristic value = service.AddCharacteristic(
            ValueUuid,
            GattProperty.Read | GattProperty.Write | GattProperty.Notify,
            onRead: (_, _) => ValueTask.FromResult(stored),
            onWrite: (_, written, _) =>
            {
                stored = written.ToArray();
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
        await windows.Peripheral.AddServiceAsync(service, Token);
        await using AdvertisingSet set = await windows.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.Empty,
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvInd,
            cancellationToken: Token
        );
        // Windows advertises the added service's UUID
        IGapAdvertisement heard = await FindAsync(
            dongle,
            advertisement => advertisement.Data.GetServiceUuids().Any(uuid => uuid.Value == ServiceUuid.Value)
        );

        await using ConnectedPeripheral connection = await dongle.Central.ConnectAsync(
            heard.Address,
            cancellationToken: Token
        );
        RemoteGattCharacteristic remote = (
            await (await connection.DiscoverServiceAsync(ServiceUuid, Token)).DiscoverCharacteristicsAsync(Token)
        ).Single(c => c.Uuid.Value == ValueUuid.Value);
        byte[] read = await remote.ReadAsync(Token);
        await remote.WriteAsync(new byte[] { 7 }, Token);
        using NotificationStream<byte[]> notifications = remote.Subscribe();
        await remote.EnableNotificationsAsync(Token);
        await value.NotifyAllAsync(new byte[] { 9 }, Token);

        read.ShouldBe([42]);
        stored.ShouldBe([7]);
        (await notifications.FirstAsync(Token).AsTask().WaitAsync(Patience, Token)).ShouldBe([9]);
    }
}
