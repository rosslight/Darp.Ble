using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Linq;
using Darp.Ble.Utils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;
using LogEvent = Darp.Ble.Logger.LogEvent;

namespace Darp.Ble.Tests;

public sealed class BleTests
{
    private static readonly byte[] AdvBytes = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB".ToByteArray();
    private readonly BleManager _manager;

    public BleTests(ITestOutputHelper outputHelper)
    {
        ILogger logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(outputHelper)
            .CreateLogger();
        _manager = new BleManagerBuilder()
            .OnLog((_, logEvent) => logger.Write((LogEventLevel)logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties))
            .With<SubstituteBleFactory>()
            .CreateManager();
    }

    private sealed class SubstituteBleFactory : IBleFactory
    {
        public IEnumerable<IBleDeviceImplementation> EnumerateDevices()
        {
            var impl = Substitute.For<IBleDeviceImplementation>();
            impl.InitializeAsync().Returns(Task.FromResult(InitializeResult.Success));
            var observer = Substitute.For<IBleObserverImplementation>();
            observer.TryStartScan(Arg.Any<BleObserver>(), out _)
                .Returns(info =>
                {
                    info[1] = Observable.Return(GapAdvertisement.FromExtendedAdvertisingReport(
                        null!,
                        DateTimeOffset.UtcNow, AdvBytes));
                    return true;
                });
            impl.Observer.Returns(observer);
            yield return impl;
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        List<(BleDevice, LogEvent)> resultList = [];
        BleManager manager = new BleManagerBuilder()
            .OnLog((bleDevice, logEvent) => resultList.Add((bleDevice, logEvent)))
            .With<SubstituteBleFactory>()
            .CreateManager();
        BleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        resultList.Should().HaveElementAt(0, (device, new LogEvent(1, null, "Adapter Initialized!", Array.Empty<object?>())));
    }

    [Fact]
    public async Task GeneralFlow()
    {

        BleDevice[] adapters = _manager.EnumerateDevices().ToArray();

        adapters.Should().ContainSingle();

        BleDevice device = adapters.First();

        device.IsInitialized.Should().BeFalse();
        device.Capabilities.Should().Be(Capabilities.None);

        InitializeResult initResult = await device.InitializeAsync();
        initResult.Should().Be(InitializeResult.Success);

        device.IsInitialized.Should().BeTrue();
        device.Capabilities.Should().HaveFlag(Capabilities.Observer);

        BleObserver observer = device.Observer;

        IGapAdvertisement<string> adv = await observer.RefCount()
            .Select(x => x.WithUserData(""))
            .Where(x => x.UserData == "")
            .Timeout(TimeSpan.FromSeconds(1))
            .FirstAsync();

        observer.IsScanning.Should().BeFalse();

        adv.AsByteArray().Should().BeEquivalentTo(AdvBytes);
        ((ulong)adv.Address.Value).Should().Be(0xAABBCCDDEEFF);
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