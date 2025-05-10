using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using Darp.Ble.Mock;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests;

public sealed class BleTests(ILoggerFactory loggerFactory)
{
    private static readonly byte[] AdvBytes =
        "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB".ToByteArray();
    private readonly BleManager _manager = new BleManagerBuilder()
        .SetLogger(loggerFactory)
        .Add<BleMockFactory>()
        .CreateManager();

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

        Task<IGapAdvertisement<string>> advTask = observer
            .OnAdvertisement()
            .Select(x => x.WithUserData(""))
            .Where(x => x.UserData.Length == 0)
            .Timeout(TimeSpan.FromSeconds(1))
            .FirstAsync()
            .ToTask();
        await observer.StartObservingAsync();
        IGapAdvertisement<string> adv = await advTask;

        observer.IsObserving.Should().BeFalse();

        adv.AsByteArray().Should().BeEquivalentTo(AdvBytes);
        ((ulong)adv.Address.Value).Should().Be(0xAABBCCDDEEFF);
    }
}
