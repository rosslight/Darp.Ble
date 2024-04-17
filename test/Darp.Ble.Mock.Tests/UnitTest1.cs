using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Utils;
using FluentAssertions;
using Microsoft.Reactive.Testing;

namespace Darp.Ble.Mock.Tests;
/*
public static class HeartRateServiceContract
{
    public static BleUuid Uuid => new(0x1234);
    public static Characteristic<Notify> HeartRateMeasurement { get; } = new(0x2A37);
    public static Characteristic<Read<byte>> BodySensorLocation { get; } = new(0x2A38);
    public static Characteristic<Write> HeartRateControlPoint { get; } = new(0x2A39);

    public static GattServerHeartRateService AddHeartRateService(this IBlePeripheral peripheral)
    {
        var service = new GattServerHeartRateService();
        peripheral.AddService(service);
        return service;
    }

    public sealed class GattClientHeartRateService
    {
        public GattClientCharacteristic<Notify> HeartRateMeasurement { get; }
    }

    public sealed class GattServerHeartRateService : IGattServerService
    {
        public IGattServerCharacteristic<Notify> HeartRateMeasurement { get; }
        public IGattServerCharacteristic<Read<byte>> BodySensorLocation { get; }
        public IGattServerCharacteristic<Write> HeartRateControlPoint { get; }
    }
}

public static class GattProperty
{
    public sealed class Notify;
    public sealed class Write;
    public sealed class Read;
    public sealed class Read<T> where T : unmanaged;
}

public interface IGattServerCharacteristic<TProperty1>
{
    
}

public static class Ex
{
    public static IDisposable NotifyAll(this IGattServerCharacteristic<Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }
    public static IDisposable UpdateReadAll<T>(this IGattServerCharacteristic<Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }
    public static IDisposable OnWrite(this IGattServerCharacteristic<Write> characteristic,
        Action<IGattClientDevice, byte[]> callback)
    {
        throw new NotImplementedException();
    }
}
*/

public sealed class UnitTest1
{
    private const string AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName = "0201010908546573744E616D65";

    private static async Task<IBleDevice> GetMockDeviceAsync(Func<MockBleBroadcaster, IBlePeripheral, Task> configure)
    {
        BleManager bleManager = new BleManagerBuilder()
            .With(new BleMockFactory { OnInitialize = configure } )
            .CreateManager();
        IBleDevice device = bleManager.EnumerateDevices().First();
        InitializeResult result = await device.InitializeAsync();
        result.Should().Be(InitializeResult.Success);
        return device;
    }

    [Fact]
    public async Task Observer_FirstAdvertisement_ShouldHaveCorrectData()
    {
        // Arrange
        var scheduler = new TestScheduler();
        AdvertisingData adData = AdvertisingData.From(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName.ToByteArray());

        // Act
        IBleDevice device = await GetMockDeviceAsync(Configure);

        Task Configure(MockBleBroadcaster broadcaster, IBlePeripheral peripheral)
        {
            IObservable<AdvertisingData> source = Observable.Interval(TimeSpan.FromMilliseconds(1000), scheduler)
                .Select(_ => adData);
            broadcaster.Advertise(source);
            return Task.CompletedTask;
        }

        IGapAdvertisement adv = await device.Observer.RefCount().FirstAsync();

        // Assert
        adv.Data.Should().BeEquivalentTo(adData);
    }

    [Fact]
    public async Task Test2()
    {
        BleManager bleManager = new BleManagerBuilder()
            .With(new BleMockFactory
            {
                OnInitialize = (broadcaster, _) =>
                {
                    /*
                    var heartRateService = factory.Peripheral.AddHeartRateService();
                    IObservable<byte[]> observable = Observable.Interval(TimeSpan.FromMilliseconds(100))
                        .Select(BitConverter.GetBytes);
                    heartRateService.HeartRateMeasurement.NotifyAll(observable);
                    heartRateService.BodySensorLocation.UpdateReadAll<byte>(42);
                    heartRateService.HeartRateControlPoint.OnWrite((_, _) => { });
    */
                    IObservable<AdvertisingData> source = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                        .Select(_ => AdvertisingData.Empty);
                    IDisposable disposable = broadcaster.Advertise(source);
                    return Task.CompletedTask;
                },
            })
            .CreateManager();
        IBleDevice device = bleManager.EnumerateDevices().First();

        await device.InitializeAsync();

        IGapAdvertisement adv = await device.Observer.RefCount().FirstAsync();
    }
}