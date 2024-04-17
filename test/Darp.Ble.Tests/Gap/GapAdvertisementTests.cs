using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Darp.Ble.Utils;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Darp.Ble.Tests.Gap;

public sealed class GapAdvertisementTests
{
    private readonly BleManager _manager;

    public GapAdvertisementTests(ITestOutputHelper outputHelper)
    {
        Serilog.Core.Logger logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(outputHelper, formatProvider: null)
            .CreateLogger();
        _manager = new BleManagerBuilder()
            .OnLog((_, logEvent) => logger.Write((LogEventLevel)logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties))
            .With<BleMockFactory>()
            .CreateManager();
    }

    [Theory]
    [InlineData(BleEventType.AdvInd, BleAddressType.Public, 0xAABBCCDDEEFF, Physical.Le1M, Physical.NotAvailable,
        AdvertisingSId.NoAdIProvided, TxPowerLevel.NotAvailable, -40, PeriodicAdvertisingInterval.NoPeriodicAdvertising,
        BleAddressType.NotAvailable, 0x000000000000,
        AdTypes.Flags, "1A", AdTypes.CompleteListOf16BitServiceClassUuids, "AABB",
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
        byte[] sectionData1 = sectionDataHex1.ToByteArray();
        byte[] sectionData2 = sectionDataHex2.ToByteArray();
        var device = _manager.EnumerateDevices().First();
        await device.InitializeAsync();

        GapAdvertisement adv = GapAdvertisement.FromExtendedAdvertisingReport(device.Observer, DateTimeOffset.UtcNow, eventType,
            new BleAddress(addressType, (UInt48)address), primaryPhy, secondaryPhy,
            advertisingSId, txPower, (Rssi)rssi, periodicAdvertisingInterval, new BleAddress(directAddressType, (UInt48)directAddress), new[]
            {
                (advertisingDataType1, sectionData1),
                (advertisingDataType2, sectionData2)
            });
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
        adv.Data.Should().HaveElementAt(0, (advertisingDataType1, sectionData1));
        adv.Data.Should().HaveElementAt(1, (advertisingDataType2, sectionData2));
    }
}