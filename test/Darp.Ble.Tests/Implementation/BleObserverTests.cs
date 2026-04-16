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
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleObserverTests(ILoggerFactory loggerFactory)
{
    private sealed class TestBleObserver(BleDevice device, ILogger<BleObserver> logger) : BleObserver(device, logger)
    {
        protected override Task StartObservingAsyncCore(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task StopObservingAsyncCore() => Task.CompletedTask;

        public Task FailAsync(Exception exception) => OnErrorAsync(exception);
    }

    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private const string AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName = "0201010908546573744E616D65";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

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
        result.ShouldBe(InitializeResult.Success);
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
        await device.Observer.StartObservingAsync(Token);
        scheduler.AdvanceTo(TimeSpan.FromMilliseconds(999).Ticks);
        task.IsCompleted.ShouldBeFalse();
        scheduler.AdvanceTo(TimeSpan.FromMilliseconds(1000).Ticks);
        task.IsCompleted.ShouldBeTrue();

        IGapAdvertisement adv = await task;
        adv.Data.ToByteArray().ShouldBe(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName.ToByteArray());
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
        await device.Observer.StartObservingAsync(Token);

        await device.Observer.StopObservingAsync();
        task1.Status.ShouldBe(TaskStatus.WaitingForActivation);
        task2.Status.ShouldBe(TaskStatus.WaitingForActivation);
        device.Observer.IsObserving.ShouldBeFalse();
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

        var exception = await act.ShouldThrowAsync<BleObservationStartException>();
        exception.InnerException.ShouldBeOfType<DummyException>();
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

        await act.ShouldThrowAsync<BleObservationException>();
    }

    [Fact]
    public async Task Configure_WhenCalled_ShouldSetParameters()
    {
        var targetParameters = new BleObservationParameters { ScanType = ScanType.Active };
        IBleDevice device = await GetMockDeviceAsync();

        device.Observer.Parameters.ShouldNotBe(targetParameters);
        device.Observer.Configure(targetParameters);

        device.Observer.Parameters.ShouldBe(targetParameters);
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
        await xx.ShouldThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task OnAdvertisement_WhenDisposed_ShouldThrowException()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        Action xx = () => device.Observer.OnAdvertisement(_ => { });
        xx.ShouldThrow<ObjectDisposedException>();
    }

    [Fact]
    public async Task OnAdvertisement_WhenObservationFails_ShouldInvokeErrorAndTerminateSubscription()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = new TestBleObserver(device, _loggerFactory.CreateLogger<BleObserver>());
        var exception = new BleObservationException(observer, "transport failed", innerException: null);
        var receivedErrors = new List<Exception>();
        var receivedAdvertisements = 0;

        observer.OnAdvertisement(_ => receivedAdvertisements++, receivedErrors.Add);

        await observer.StartObservingAsync(Token);
        await observer.FailAsync(exception);

        receivedAdvertisements.ShouldBe(0);
        receivedErrors.ShouldHaveSingleItem();
        receivedErrors[0].ShouldBe(exception);
    }

    [Fact]
    public async Task OnAdvertisementObservable_WhenObservationFails_ShouldFault()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = new TestBleObserver(device, _loggerFactory.CreateLogger<BleObserver>());
        var exception = new BleObservationException(observer, "transport failed", innerException: null);
        Task<IGapAdvertisement> task = observer.OnAdvertisement().FirstAsync().ToTask();
        await observer.StartObservingAsync(Token);

        await observer.FailAsync(exception);

        var actualException = await Should.ThrowAsync<BleObservationException>(() => task);
        actualException.ShouldBe(exception);
    }

    [Fact]
    public async Task OnAdvertisementObservable_WhenNotStartedAndObservationFails_ShouldFault()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = new TestBleObserver(device, _loggerFactory.CreateLogger<BleObserver>());
        var exception = new BleObservationException(observer, "transport failed", innerException: null);
        Task<IGapAdvertisement> task = observer.OnAdvertisement().FirstAsync().ToTask();

        await observer.FailAsync(exception);

        var actualException = await Should.ThrowAsync<BleObservationException>(() => task);
        actualException.ShouldBe(exception);
    }

    [Fact]
    public async Task OnAdvertisementObservable_WhenStoppedAndObservationFails_ShouldFault()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = new TestBleObserver(device, _loggerFactory.CreateLogger<BleObserver>());
        var exception = new BleObservationException(observer, "transport failed", innerException: null);
        Task<IGapAdvertisement> task = observer.OnAdvertisement().FirstAsync().ToTask();
        await observer.StartObservingAsync(Token);
        await observer.StopObservingAsync();

        await observer.FailAsync(exception);

        var actualException = await Should.ThrowAsync<BleObservationException>(() => task);
        actualException.ShouldBe(exception);
    }

    [Fact(Timeout = 5000)]
    public async Task PublishObservable_WhenObservationFails_ShouldFault()
    {
        var device = Substitute.For<BleDevice>(null!, null!);
        var observer = new TestBleObserver(device, _loggerFactory.CreateLogger<BleObserver>());
        var exception = new BleObservationException(observer, "transport failed", innerException: null);
        Task<IGapAdvertisement> task = observer.Publish().RefCount().FirstAsync().ToTask();

        await observer.FailAsync(exception);

        var actualException = await Should.ThrowAsync<BleObservationException>(() => task);
        actualException.ShouldBe(exception);
    }
}
