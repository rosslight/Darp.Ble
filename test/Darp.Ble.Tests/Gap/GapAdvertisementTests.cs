using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using Darp.Ble.Mock;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests.Gap;

public sealed class GapAdvertisementTests(ILoggerFactory loggerFactory)
{
    private readonly BleManager _manager = new BleManagerBuilder()
        .SetLogger(loggerFactory)
        .Add<BleMockFactory>()
        .CreateManager();

    [Theory]
    [InlineData(BleEventType.AdvInd, BleAddressType.Public, 0xAABBCCDDEEFF, Physical.Le1M, Physical.NotAvailable,
        AdvertisingSId.NoAdIProvided, TxPowerLevel.NotAvailable, -40, PeriodicAdvertisingInterval.NoPeriodicAdvertising,
        BleAddressType.NotAvailable, 0x000000000000,
        AdTypes.Flags, "1A", AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, "AABB",
        "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB")]
    public async Task Advertisement_FromExtendedAdvertisingReport(BleEventType eventType, BleAddressType addressType, ulong address,
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
        string expectedReportHex)
    {
        byte[] sectionData1 = Convert.FromHexString(sectionDataHex1);
        byte[] sectionData2 = Convert.FromHexString(sectionDataHex2);
        IBleDevice device = _manager.EnumerateDevices().First();
        await device.InitializeAsync();

        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(device.Observer,
            DateTimeOffset.UtcNow,
            eventType,
            new BleAddress(addressType, (UInt48)address),
            primaryPhy, secondaryPhy,
            advertisingSId, txPower, (Rssi)rssi, periodicAdvertisingInterval, new BleAddress(directAddressType, (UInt48)directAddress),
            AdvertisingData.From(
            [
                (advertisingDataType1, sectionData1),
                (advertisingDataType2, sectionData2),
            ]));
        string byteString = Convert.ToHexString(adv.AsByteArray());

        byteString.Should().Be(expectedReportHex);
        adv.EventType.Should().Be(eventType);
        adv.Address.Type.Should().Be(addressType);
        adv.Address.Value.Should().Be((UInt48)address);
        adv.PrimaryPhy.Should().Be(primaryPhy);
        adv.SecondaryPhy.Should().Be(secondaryPhy);
        adv.AdvertisingSId.Should().Be(advertisingSId);
        adv.TxPower.Should().Be(txPower);
        adv.Rssi.Should().Be((Rssi)rssi);
        adv.PeriodicAdvertisingInterval.Should().Be(periodicAdvertisingInterval);
        adv.DirectAddress.Type.Should().Be(directAddressType);
        adv.DirectAddress.Value.Should().Be((UInt48)directAddress);
        adv.Data.Should().HaveCount(2);
        (AdTypes Type, ReadOnlyMemory<byte> Bytes) dataSection1 = adv.Data[0];
        dataSection1.Type.Should().Be(advertisingDataType1);
        dataSection1.Bytes.ToArray().Should().BeEquivalentTo(sectionData1);
        (AdTypes Type, ReadOnlyMemory<byte> Bytes) dataSection2 = adv.Data[1];
        dataSection2.Type.Should().Be(advertisingDataType2);
        dataSection2.Bytes.ToArray().Should().BeEquivalentTo(sectionData2);
    }

    [Theory]
    [InlineData("130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB")]
    public void WithUserData_WithTestData_ShouldHaveEquivalentValues(string reportHex)
    {
        const int testData = 12345;
        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(null!, DateTimeOffset.UtcNow, reportHex.ToByteArray());

        IGapAdvertisement<int> advWithData = adv.WithUserData(testData);

        advWithData.Observer.Should().Be(adv.Observer);
        advWithData.Timestamp.Should().Be(adv.Timestamp);
        advWithData.EventType.Should().Be(adv.EventType);
        advWithData.Address.Should().Be(adv.Address);
        advWithData.PrimaryPhy.Should().Be(adv.PrimaryPhy);
        advWithData.SecondaryPhy.Should().Be(adv.SecondaryPhy);
        advWithData.AdvertisingSId.Should().Be(adv.AdvertisingSId);
        advWithData.TxPower.Should().Be(adv.TxPower);
        advWithData.Rssi.Should().Be(adv.Rssi);
        advWithData.PeriodicAdvertisingInterval.Should().Be(adv.PeriodicAdvertisingInterval);
        advWithData.DirectAddress.Should().Be(adv.DirectAddress);
        advWithData.Data.Should().BeEquivalentTo(adv.Data);
        advWithData.UserData.Should().Be(testData);
        (advWithData as IGapAdvertisementWithUserData)?.UserData.Should().Be(testData);
        advWithData.AsByteArray().Should().BeEquivalentTo(adv.AsByteArray());
    }
}