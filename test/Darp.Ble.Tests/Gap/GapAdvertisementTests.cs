using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class GapAdvertisementTests
{
    [Theory]
    [InlineData(
        BleEventType.AdvInd,
        BleAddressType.Public,
        0xAABBCCDDEEFF,
        Physical.Le1M,
        Physical.NotAvailable,
        AdvertisingSId.NoAdIProvided,
        TxPowerLevel.NotAvailable,
        -40,
        PeriodicAdvertisingInterval.NoPeriodicAdvertising,
        BleAddressType.NotAvailable,
        0x000000000000,
        AdTypes.Flags,
        "1A",
        AdTypes.CompleteListOf16BitServiceOrServiceClassUuids,
        "AABB",
        "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB"
    )]
    [InlineData(
        BleEventType.AdvNonConnInd,
        BleAddressType.Random,
        0x5A1122334455,
        Physical.Le1M,
        Physical.NotAvailable,
        AdvertisingSId.NoAdIProvided,
        TxPowerLevel.NotAvailable,
        -40,
        PeriodicAdvertisingInterval.NoPeriodicAdvertising,
        BleAddressType.NotAvailable,
        0x000000000000,
        AdTypes.Flags,
        "1A",
        AdTypes.CompleteListOf16BitServiceOrServiceClassUuids,
        "AABB",
        "10000155443322115A0100FF7FD80000FF0000000000000702011A0303AABB"
    )]
    public void Advertisement_FromExtendedAdvertisingReport(
        BleEventType eventType,
        BleAddressType addressType,
        ulong address,
        Physical primaryPhy,
        Physical secondaryPhy,
        AdvertisingSId advertisingSId,
        TxPowerLevel txPower,
        sbyte rssi,
        PeriodicAdvertisingInterval periodicAdvertisingInterval,
        BleAddressType directAddressType,
        ulong directAddress,
        AdTypes advertisingDataType1,
        string sectionDataHex1,
        AdTypes advertisingDataType2,
        string sectionDataHex2,
        string expectedReportHex
    )
    {
        byte[] sectionData1 = Convert.FromHexString(sectionDataHex1);
        byte[] sectionData2 = Convert.FromHexString(sectionDataHex2);

        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(
            DateTimeOffset.UtcNow,
            eventType,
            new BleAddress(addressType, (UInt48)address),
            primaryPhy,
            secondaryPhy,
            advertisingSId,
            txPower,
            (Rssi)rssi,
            periodicAdvertisingInterval,
            new BleAddress(directAddressType, (UInt48)directAddress),
            AdvertisingData.From([(advertisingDataType1, sectionData1), (advertisingDataType2, sectionData2)])
        );
        string byteString = Convert.ToHexString(adv.AsByteArray());

        byteString.ShouldBe(expectedReportHex);
        adv.EventType.ShouldBe(eventType);
        adv.Address.Type.ShouldBe(addressType);
        adv.Address.Value.ShouldBe((UInt48)address);
        adv.PrimaryPhy.ShouldBe(primaryPhy);
        adv.SecondaryPhy.ShouldBe(secondaryPhy);
        adv.AdvertisingSId.ShouldBe(advertisingSId);
        adv.TxPower.ShouldBe(txPower);
        adv.Rssi.ShouldBe((Rssi)rssi);
        adv.PeriodicAdvertisingInterval.ShouldBe(periodicAdvertisingInterval);
        adv.DirectAddress.Type.ShouldBe(directAddressType);
        adv.DirectAddress.Value.ShouldBe((UInt48)directAddress);
        adv.Data.Count.ShouldBe(2);
        (AdTypes Type, ReadOnlyMemory<byte> Bytes) dataSection1 = adv.Data[0];
        dataSection1.Type.ShouldBe(advertisingDataType1);
        dataSection1.Bytes.ToArray().ShouldBe(sectionData1);
        (AdTypes Type, ReadOnlyMemory<byte> Bytes) dataSection2 = adv.Data[1];
        dataSection2.Type.ShouldBe(advertisingDataType2);
        dataSection2.Bytes.ToArray().ShouldBe(sectionData2);
    }

    [Theory]
    [InlineData(0x00, BleAddressType.Public)]
    [InlineData(0x01, BleAddressType.Random)]
    [InlineData(0x02, BleAddressType.PublicIdentity)]
    [InlineData(0x03, BleAddressType.RandomStaticIdentity)]
    [InlineData(0xFF, BleAddressType.NotAvailable)]
    public void FromExtendedAdvertisingReport_WithHciAddressType_ShouldHaveAddressType(
        byte hciAddressType,
        BleAddressType expectedAddressType
    )
    {
        byte[] bytes = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB".ToByteArray();
        bytes[2] = hciAddressType;

        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(DateTimeOffset.UtcNow, bytes);

        adv.Address.Type.ShouldBe(expectedAddressType);
    }

    [Theory]
    [InlineData("130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB")]
    public void WithUserData_WithTestData_ShouldHaveEquivalentValues(string reportHex)
    {
        const int testData = 12345;
        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(
            DateTimeOffset.UtcNow,
            reportHex.ToByteArray()
        );

        IGapAdvertisement<int> advWithData = adv.WithUserData(testData);

        advWithData.Timestamp.ShouldBe(adv.Timestamp);
        advWithData.EventType.ShouldBe(adv.EventType);
        advWithData.Address.ShouldBe(adv.Address);
        advWithData.PrimaryPhy.ShouldBe(adv.PrimaryPhy);
        advWithData.SecondaryPhy.ShouldBe(adv.SecondaryPhy);
        advWithData.AdvertisingSId.ShouldBe(adv.AdvertisingSId);
        advWithData.TxPower.ShouldBe(adv.TxPower);
        advWithData.Rssi.ShouldBe(adv.Rssi);
        advWithData.PeriodicAdvertisingInterval.ShouldBe(adv.PeriodicAdvertisingInterval);
        advWithData.DirectAddress.ShouldBe(adv.DirectAddress);
        advWithData.Data.ShouldBe(adv.Data);
        advWithData.UserData.ShouldBe(testData);
        (advWithData as IGapAdvertisementWithUserData)?.UserData.ShouldBe(testData);
        advWithData.AsByteArray().ShouldBe(adv.AsByteArray());
    }
}
