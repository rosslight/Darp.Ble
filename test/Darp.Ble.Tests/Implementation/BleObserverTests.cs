using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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
        await using IDisposableObservable<IGapAdvertisement> advObservable =
            await device.Observer.StartObservingAsync();
        Task<IGapAdvertisement> task = advObservable.FirstAsync().ToTask();
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
    public async Task StopScan_MultipleConnections_ShouldHaltAll()
    {
        // Arrange
        var scheduler = new TestScheduler();
        IBleDevice device = await Get1000MsAdvertisementMockDeviceAsync(scheduler);

        // Act
        Task<IGapAdvertisement> task1 = (await device.Observer.StartObservingAsync()).ToTask();
        Task<IGapAdvertisement> task2 = (await device.Observer.StartObservingAsync()).ToTask();

        await device.Observer.StopObservingAsync();
        task1.Status.Should().Be(TaskStatus.Faulted);
        task2.Status.Should().Be(TaskStatus.Faulted);
        device.Observer.IsScanning.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Argument specification", "NS3005:Could not set argument.")]
    public async Task Observer_WhenFailedWithAnyException_ShouldReturnException()
    {
        var observer = Substitute.For<BleObserver>(null, null);
        observer
            .InvokeNonPublicMethod("TryStartScanCore", Observable.Empty<IGapAdvertisement>())
            .ReturnsForAnyArgs(info =>
            {
                info[0] = Observable.Throw<IGapAdvertisement>(new DummyException("Dummy"));
                return false;
            });

        Func<Task> act = async () => _ = (await observer.StartObservingAsync()).FirstAsync().Subscribe();

        await act.Should()
            .ThrowAsync<BleObservationException>()
            .Where(x => typeof(DummyException).IsAssignableTo(x.InnerException!.GetType()));
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Argument specification", "NS3005:Could not set argument.")]
    public async Task Observer_WhenFailedWithBleObservationException_ShouldReturnException()
    {
        var observer = Substitute.For<BleObserver>(null, null);
        observer
            .InvokeNonPublicMethod("TryStartScanCore", Observable.Empty<IGapAdvertisement>())
            .ReturnsForAnyArgs(info =>
            {
                info[0] = Observable.Throw<IGapAdvertisement>(
                    new BleObservationException(observer!, message: null, innerException: null)
                );
                return false;
            });

        Func<Task> act = async () => _ = (await observer.StartObservingAsync()).FirstAsync().Subscribe();

        await act.Should().ThrowAsync<BleObservationException>();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Argument specification", "NS3005:Could not set argument.")]
    public async Task Connect_WhenFailed_ShouldFaultAndReturnEmptyDisposable()
    {
        var observer = Substitute.For<BleObserver>(null, null);
        observer
            .InvokeNonPublicMethod("TryStartScanCore", Observable.Empty<IGapAdvertisement>())
            .ReturnsForAnyArgs(info =>
            {
                info[0] = Observable.Throw<IGapAdvertisement>(new DummyException("Dummy"));
                return false;
            });

        await using IDisposableObservable<IGapAdvertisement> advObservable = await observer.StartObservingAsync();
        Task<IGapAdvertisement> task = advObservable.FirstAsync().ToTask();
        task.Status.Should().Be(TaskStatus.Faulted);
        task.Exception?.InnerException.Should().BeOfType<BleObservationException>();
        task.Exception?.InnerException?.InnerException.Should().BeOfType<DummyException>();
        advObservable.Should().Be(Disposable.Empty);
    }

    [Fact]
    public async Task Configure_WhenCalled_ShouldSetParameters()
    {
        var targetParameters = new BleScanParameters { ScanType = ScanType.Active };
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
    public async Task Connect_WhenConnectedTwice_ShouldReturnSame()
    {
        IBleDevice device = await GetMockDeviceAsync();

        IAsyncDisposable disposable1 = await device.Observer.StartObservingAsync();
        IAsyncDisposable disposable2 = await device.Observer.StartObservingAsync();

        disposable1.Should().Be(disposable2);
    }

    [Fact]
    public async Task Subscribe_WhenDisposed_ShouldThrowException()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        Func<Task> xx = async () => _ = await device.Observer.StartObservingAsync();
        await xx.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Connect_WhenDisposed_ShouldReturnEmpty()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();

        IAsyncDisposable disposable = await device.Observer.StartObservingAsync();
        disposable.Should().Be(Disposable.Empty);
    }
}
