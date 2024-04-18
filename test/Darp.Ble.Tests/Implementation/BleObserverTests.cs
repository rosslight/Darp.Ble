using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Darp.Ble.Utils;
using FluentAssertions;
using Microsoft.Reactive.Testing;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleObserverTests
{
    private const string AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName = "0201010908546573744E616D65";

    private static async Task<IBleDevice> GetMockDeviceAsync(BleMockFactory.InitializeAsync configure)
    {
        BleManager bleManager = new BleManagerBuilder()
            .With(new BleMockFactory { OnInitialize = configure } )
            .CreateManager();
        IBleDevice device = bleManager.EnumerateDevices().First();
        InitializeResult result = await device.InitializeAsync();
        result.Should().Be(InitializeResult.Success);
        return device;
    }

    private static async Task<IBleDevice> Get1000MsAdvertisementMockDeviceAsync(IScheduler scheduler)
    {
        AdvertisingData adData = AdvertisingData.From(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName.ToByteArray());
        return await GetMockDeviceAsync(Configure);

        Task Configure(IBleBroadcaster broadcaster, IBlePeripheral _)
        {
            IObservable<AdvertisingData> source = Observable.Interval(TimeSpan.FromMilliseconds(1000), scheduler)
                .Select(_ => adData);
            broadcaster.Advertise(source);
            return Task.CompletedTask;
        }
    }

    private static async Task<IBleDevice> GetMockDeviceAsync()
    {
        return await GetMockDeviceAsync((_, _) => Task.CompletedTask);
    }

    [Fact]
    public async Task Observer_FirstAdvertisement_ShouldHaveCorrectData()
    {
        // Arrange
        var scheduler = new TestScheduler();
        IBleDevice device = await Get1000MsAdvertisementMockDeviceAsync(scheduler);

        // Act
        Task<IGapAdvertisement> task = device.Observer.RefCount().FirstAsync().ToTask();
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
    public async Task DisposeAsync_WhenDisposedTwice_ShouldIgnore()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        await device.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_WhenDisposed_ShouldThrowException()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();
        Action xx = () => _ = device.Observer.Subscribe();
        xx.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Connect_WhenDisposed_ShouldReturnEmpty()
    {
        IBleDevice device = await GetMockDeviceAsync();

        await device.DisposeAsync();

        IDisposable disposable = device.Observer.Connect();
        disposable.Should().Be(Disposable.Empty);
    }
    /*



     */
}