using System.Reactive.Linq;
using Darp.Ble.Implementation;
using FluentAssertions;
using NSubstitute;

namespace Darp.Ble.Tests;

public class UnitTest1
{
    private sealed class Xyz : IBleImplementation
    {
        public IEnumerable<BleDevice> EnumerateAdapters()
        {
            var impl = Substitute.For<IBleDeviceImplementation>();
            impl.InitializeAsync().Returns(Task.FromResult(InitializeResult.Success));
            var observer = Substitute.For<IBleObserverImplementation>();
            observer.TryStartScan(out Arg.Any<IObservable<IGapAdvertisement>?>())
                .Returns(info =>
                {
                    info[0] = Observable.Return(Substitute.For<IGapAdvertisement>());
                    return true;
                });
            impl.Observer.Returns(observer);
            yield return new BleDevice(impl);
        }
    }

    [Fact]
    public async Task Test1()
    {
        BleManager manager = new BleManagerBuilder()
            .WithImplementation<Xyz>()
            .CreateManager();

        var adapters = manager.EnumerateDevices().ToArray();

        adapters.Should().ContainSingle();

        BleDevice device = adapters.First();

        device.IsInitialized.Should().BeFalse();
        device.Capabilities.Should().Be(Capabilities.Unknown);

        InitializeResult initResult = await device.InitializeAsync();
        initResult.Should().Be(InitializeResult.Success);

        device.IsInitialized.Should().BeTrue();
        device.Capabilities.Should().HaveFlag(Capabilities.Observer);

        BleObserver observer = device.Observer;

        await observer.RefCount().FirstAsync();

        observer.IsScanning.Should().BeFalse();
    }
}