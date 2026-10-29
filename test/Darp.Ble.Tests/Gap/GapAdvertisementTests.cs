using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class GapAdvertisementTests
{
    private static readonly AdvertisingData FlagsAndServices = AdvertisingData.From([
        (AdTypes.Flags, [0x1A]),
        (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [0xBB, 0xAA]),
    ]);

    internal static GapAdvertisement Create(AdvertisingData data, DateTimeOffset? timestamp = null) =>
        GapAdvertisement.Create(
            timestamp ?? DateTimeOffset.UnixEpoch,
            BleEventType.AdvInd,
            new BleAddress(BleAddressType.Public, (UInt48)0xAABBCCDDEEFF),
            Physical.Le1M,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            TxPowerLevel.NotAvailable,
            (Rssi)(-40),
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            BleAddress.NotAvailable,
            data
        );

    [Fact]
    public void Create_KeepsDataLongerThanOneHciReport()
    {
        // Ten sections of 31 bytes: 310 bytes, more than the 255 a single HCI report could describe
        AdvertisingData data = AdvertisingData.From([
            .. Enumerable.Range(0, 10).Select(i => (AdTypes.ManufacturerSpecificData, new byte[29])),
        ]);

        GapAdvertisement advertisement = Create(data);

        advertisement.Data.AsReadOnlyMemory().Length.ShouldBe(310);
        advertisement.Data.Count.ShouldBe(10);
    }

    [Fact]
    public void EqualAdvertisements_HaveTheSameHashCode()
    {
        HashSet<IGapAdvertisement> seen = [Create(FlagsAndServices)];

        seen.Contains(Create(FlagsAndServices)).ShouldBeTrue();
    }

    [Fact]
    public void AdvertisementsWithOtherData_AreNotEqual()
    {
        GapAdvertisement other = Create(AdvertisingData.From([(AdTypes.Flags, [0x06])]));

        Create(FlagsAndServices).Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void WithUserData_KeepsTheAdvertisementsValues()
    {
        GapAdvertisement advertisement = Create(FlagsAndServices);

        IGapAdvertisement<int> withData = advertisement.WithUserData(12345);

        withData.UserData.ShouldBe(12345);
        (withData as IGapAdvertisementWithUserData)?.UserData.ShouldBe(12345);
        withData.Timestamp.ShouldBe(advertisement.Timestamp);
        withData.Address.ShouldBe(advertisement.Address);
        withData.Rssi.ShouldBe(advertisement.Rssi);
        withData.Data.ShouldBe(advertisement.Data);
    }
}
