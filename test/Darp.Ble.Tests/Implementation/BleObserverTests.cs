using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Mock;
using Darp.Ble.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using NSubstitute;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleObserverTests(ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private const string AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName = "0201010908546573744E616D65";

    private async Task<IBleDevice> GetMockDeviceAsync(
        BleMockFactory.InitializeSimpleAsync configure,
        IScheduler scheduler
    )
    {
        BleManager bleManager = new BleManagerBuilder()
            .SetLogger(_loggerFactory)
            .Add<BleMockFactory>(factory =>
            {
                factory.AddPeripheral(configure);
                factory.Scheduler = scheduler;
            })
            .CreateManager();
        IBleDevice device = bleManager.EnumerateDevices().First();
        InitializeResult result = await device.InitializeAsync();
        result.Should().Be(InitializeResult.Success);
        return device;
    }

    private async Task<IBleDevice> Get1000MsAdvertisementMockDeviceAsync(IScheduler scheduler)
    {
        AdvertisingData adData = AdvertisingData.From(
            AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName.ToByteArray()
        );
        return await GetMockDeviceAsync(Configure, scheduler);

        async Task Configure(IBleDevice d)
        {
            await d.Broadcaster.StartAdvertisingAsync(data: adData, interval: ScanTiming.Ms1000);
        }
    }

    private async Task<IBleDevice> GetMockDeviceAsync()
    {
        return await GetMockDeviceAsync(_ => Task.CompletedTask, Scheduler.Default);
    }

    [Fact]
    public async Task Observer_FirstAdvertisement_ShouldHaveCorrectData()
    {
        // Arrange
        var scheduler = new TestScheduler();
        IBleDevice device = await Get1000MsAdvertisementMockDeviceAsync(scheduler);

        // Act
        Task<IGapAdvertisement> task = device.Observer.OnAdvertisement().FirstAsync().ToTask();
        await device.Observer.StartObservingAsync();
        scheduler.AdvanceTo(TimeSpan.FromMilliseconds(999).Ticks);
        task.IsCompleted.Should().BeFalse();
        scheduler.AdvanceTo(TimeSpan.FromMilliseconds(1000).Ticks);
        task.IsCompleted.Should().BeTrue();

        IGapAdvertisement adv = await task;
        adv.Data.ToByteArray()
            .Should()
            .BeEquivalentTo(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName.ToByteArray());
    }

    [Fact]
    public async Task StopScan_MultipleConnections_ShouldNotHaltAll()
    {
        // Arrange
        var scheduler = new TestScheduler();
        IBleDevice device = await Get1000MsAdvertisementMockDeviceAsync(scheduler);

        // Act
        Task<IGapAdvertisement> task1 = device.Observer.OnAdvertisement().FirstAsync().ToTask();
        Task<IGapAdvertisement> task2 = device.Observer.OnAdvertisement().FirstAsync().ToTask();
        await device.Observer.StartObservingAsync();

        await device.Observer.StopObservingAsync();
        task1.Status.Should().Be(TaskStatus.WaitingForActivation);
        task2.Status.Should().Be(TaskStatus.WaitingForActivation);
        device.Observer.IsObserving.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Argument specification", "NS3005:Could not set argument.")]
    public async Task Observer_WhenFailedWithAnyException_ShouldReturnException()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = Substitute.For<BleObserver>(device, null);
        observer
            .InvokeNonPublicMethod("StartObservingAsyncCore", CancellationToken.None)
            .ReturnsForAnyArgs(_ => throw new DummyException("Lorem ipsum"));

        Func<Task> act = async () => await observer.StartObservingAsync();

        await act.Should()
            .ThrowAsync<BleObservationStartException>()
            .Where(x => typeof(DummyException).IsAssignableTo(x.InnerException!.GetType()));
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Argument specification", "NS3005:Could not set argument.")]
    public async Task Observer_WhenFailedWithBleObservationException_ShouldReturnException()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = Substitute.For<BleObserver>(device, null);
        observer
            .InvokeNonPublicMethod("StartObservingAsyncCore", CancellationToken.None)
            .ReturnsForAnyArgs(_ => throw new BleObservationException(observer!, message: null, innerException: null));

        Func<Task> act = async () => await observer.StartObservingAsync();

        await act.Should().ThrowAsync<BleObservationException>();
    }

    [Fact]
    public async Task Configure_WhenCalled_ShouldSetParameters()
    {
        var targetParameters = new BleObservationParameters { ScanType = ScanType.Active };
        IBleDevice device = await GetMockDeviceAsync();

        device.Observer.Parameters.Should().NotBe(targetParameters);
        device.Observer.Configure(targetParameters);

        device.Observer.Parameters.Should().Be(targetParameters);
    }

    [Fact]
    public async Task DisposeAsync_WhenDisposedTwice_ShouldIgnore()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        await device.DisposeAsync();
    }

    [Fact]
    public async Task StartObservingAsync_WhenDisposed_ShouldThrowException()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        Func<Task> xx = async () => await device.Observer.StartObservingAsync();
        await xx.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task OnAdvertisement_WhenDisposed_ShouldThrowException()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        Action xx = () => device.Observer.OnAdvertisement(_ => { });
        xx.Should().Throw<ObjectDisposedException>();
    }
}
