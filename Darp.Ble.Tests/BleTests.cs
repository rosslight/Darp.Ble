using System.Reactive.Linq;
using Darp.Ble.Device;
using Darp.Ble.Implementation;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;
using LogEvent = Darp.Ble.Logger.LogEvent;

namespace Darp.Ble.Tests;

public sealed class BleTests(ITestOutputHelper outputHelper)
{
    private readonly ILogger _logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.TestOutput(outputHelper)
        .CreateLogger();

    private sealed class SubstituteBleImplementation : IBleImplementation
    {
        public IEnumerable<IBleDeviceImplementation> EnumerateAdapters()
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
            yield return impl;
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        List<(BleDevice, LogEvent)> resultList = [];
        BleManager manager = new BleManagerBuilder()
            .OnLog((device, logEvent) => resultList.Add((device, logEvent)))
            .WithImplementation<SubstituteBleImplementation>()
            .CreateManager();
        BleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        resultList.Should().HaveElementAt(0, (device, new LogEvent(1, null, "Adapter Initialized!", Array.Empty<object?>())));
    }

    [Fact]
    public async Task GeneralFlow()
    {
        BleManager manager = new BleManagerBuilder()
            .OnLog((_, logEvent) => _logger.Write((LogEventLevel)logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties))
            .WithImplementation<SubstituteBleImplementation>()
            .CreateManager();

        BleDevice[] adapters = manager.EnumerateDevices().ToArray();

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