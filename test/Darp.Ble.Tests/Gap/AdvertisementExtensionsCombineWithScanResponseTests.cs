using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementExtensionsCombineWithScanResponseTests(ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    private async Task<IBleObserver> CreateBleObserverAsync(ScanType scanType)
    {
        BleManager manager = new BleManagerBuilder().SetLogger(_loggerFactory).AddMock().CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        device.Observer.Configure(new BleObservationParameters { ScanType = scanType });
        return device.Observer;
    }

    private static BleAddress CreateAddress(ulong value = 0xAABBCCDDEEFF) => new(BleAddressType.Public, (UInt48)value);

    private static GapAdvertisement CreateAdv(
        IBleObserver observer,
        BleEventType type,
        AdvertisingSId sid,
        BleAddress address,
        string adDataHex
    )
    {
        return GapAdvertisement.FromExtendedAdvertisingReport(
            observer,
            DateTimeOffset.UtcNow,
            type,
            address,
            Physical.Le1M,
            Physical.NotAvailable,
            sid,
            TxPowerLevel.NotAvailable,
            (Rssi)(-50),
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            BleAddress.NotAvailable,
            AdvertisingData.From(Convert.FromHexString(adDataHex))
        );
    }

    [Fact]
    public async Task CombineWithScanResponse_WhenActiveScan_ShouldCombineAndEmitSingle()
    {
        // Arrange
        IBleObserver observer = await CreateBleObserverAsync(ScanType.Active);
        BleAddress addr = CreateAddress();
        const AdvertisingSId sid = AdvertisingSId.NoAdIProvided;
        GapAdvertisement adv = CreateAdv(observer, BleEventType.AdvScanInd, sid, addr, "020101"); // scannable
        GapAdvertisement scanRsp = CreateAdv(observer, BleEventType.ScanResponse, sid, addr, "050954657374"); // name "Test"

        // Act
        IGapAdvertisement[] result = await new[] { adv, scanRsp }.ToObservable().CombineWithScanResponse().ToArray();

        // Assert
        result.ShouldHaveSingleItem();

        var combined = result[0].ShouldBeAssignableTo<IGapAdvertisementWithScanResponse>();
        combined.EventType.ShouldBe(adv.EventType);
        combined.Address.ShouldBe(adv.Address);
        combined.Data.ToByteArray().ShouldBe("020101050954657374".ToByteArray());
        combined.AsByteArray().ShouldBe(adv.AsByteArray());
    }

    [Fact]
    public async Task CombineWithScanResponse_WhenPassiveScan_ShouldPassThroughBoth()
    {
        // Arrange
        IBleObserver observer = await CreateBleObserverAsync(ScanType.Passive);
        BleAddress addr = CreateAddress();
        const AdvertisingSId sid = AdvertisingSId.NoAdIProvided;
        GapAdvertisement adv = CreateAdv(observer, BleEventType.AdvScanInd, sid, addr, "020101");
        GapAdvertisement scanRsp = CreateAdv(observer, BleEventType.ScanResponse, sid, addr, "090954657374");

        // Act
        IGapAdvertisement[] result = await new[] { adv, scanRsp }.ToObservable().CombineWithScanResponse().ToArray();

        // Assert
        result.Length.ShouldBe(2);
        result[0].ShouldBe(adv);
        result[1].ShouldBe(scanRsp);
    }

    [Fact]
    public async Task CombineWithScanResponse_WhenScanResponseWithoutPrior_ShouldEmitNothing()
    {
        // Arrange
        IBleObserver observer = await CreateBleObserverAsync(ScanType.Active);
        BleAddress addr = CreateAddress();
        const AdvertisingSId sid = AdvertisingSId.NoAdIProvided;
        GapAdvertisement scanRsp = CreateAdv(observer, BleEventType.ScanResponse, sid, addr, "090954657374");

        // Act
        IGapAdvertisement[] result = await new[] { scanRsp }.ToObservable().CombineWithScanResponse().ToArray();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task CombineWithScanResponse_WhenNonScannableAdv_ShouldPassThrough()
    {
        // Arrange
        IBleObserver observer = await CreateBleObserverAsync(ScanType.Active);
        BleAddress addr = CreateAddress();
        const AdvertisingSId sid = AdvertisingSId.NoAdIProvided;
        GapAdvertisement nonScannable = CreateAdv(observer, BleEventType.AdvNonConnInd, sid, addr, "020101");

        // Act
        IGapAdvertisement[] result = await new[] { nonScannable }.ToObservable().CombineWithScanResponse().ToArray();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe(nonScannable);
    }
}
