using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using Darp.Ble.Mock;
using Darp.Ble.Utils;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Darp.Ble.Tests;

public sealed class BleTests
{
    private static readonly byte[] AdvBytes = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB".ToByteArray();
    private readonly BleManager _manager;

    public BleTests(ITestOutputHelper outputHelper)
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

    //[Fact]
    public async Task GeneralFlow()
    {
        IBleDevice[] adapters = _manager.EnumerateDevices().ToArray();

        adapters.Should().ContainSingle();

        IBleDevice device = adapters[0];

        device.IsInitialized.Should().BeFalse();
        device.Capabilities.Should().Be(Capabilities.None);

        InitializeResult initResult = await device.InitializeAsync();
        initResult.Should().Be(InitializeResult.Success);

        device.IsInitialized.Should().BeTrue();
        device.Capabilities.Should().HaveFlag(Capabilities.Observer);

        IBleObserver observer = device.Observer;

        IGapAdvertisement<string> adv = await observer.RefCount()
            .Select(x => x.WithUserData(""))
            .Where(x => x.UserData.Length == 0)
            .Timeout(TimeSpan.FromSeconds(1))
            .FirstAsync();

        observer.IsScanning.Should().BeFalse();

        adv.AsByteArray().Should().BeEquivalentTo(AdvBytes);
        ((ulong)adv.Address.Value).Should().Be(0xAABBCCDDEEFF);
    }
}