using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock;
using FluentAssertions;
using FluentAssertions.Reactive;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleCentralTests
{
    private static async Task<IBleDevice> GetMockDeviceAsync(BleMockFactory.InitializeSimpleAsync? configure = null)
    {
        configure ??= _ => Task.CompletedTask;
        BleManager bleManager = new BleManagerBuilder()
            .Add<BleMockFactory>(factory => factory.AddPeripheral(configure))
            .CreateManager();
        IBleDevice device = bleManager.EnumerateDevices().First();
        InitializeResult result = await device.InitializeAsync();
        result.Should().Be(InitializeResult.Success);
        return device;
    }

    private static async Task<IBleDevice> Get1000MsAdvertisementMockDeviceAsync(IScheduler scheduler)
    {
        return await GetMockDeviceAsync(Configure);

        async Task Configure(IBleDevice device)
        {
            IObservable<AdvertisingData> source = Observable
                .Interval(TimeSpan.FromMilliseconds(1000), scheduler)
                .Select(_ => AdvertisingData.Empty);
            device.Peripheral.AddService(0x1234);
            IAdvertisingSet set = await device.Broadcaster.CreateAdvertisingSetAsync();
            source.Subscribe(data =>
            {
                _ = Task.Run(async () =>
                {
                    await set.SetAdvertisingDataAsync(data);
                    await device.Broadcaster.StartAdvertisingAsync([(set, TimeSpan.Zero, 1)], default);
                });
            });
        }
    }

    [Theory]
    [InlineData((ConnectionTiming)5, false)]
    [InlineData(ConnectionTiming.MinValue, true)]
    [InlineData(ConnectionTiming.MaxValue, true)]
    [InlineData((ConnectionTiming)3201, false)]
    public async Task ConnectToPeripheral_BleConnectionParameters_WithDifferentParameters(
        ConnectionTiming connectionInterval,
        bool expectedResult
    )
    {
        var address = new BleAddress(BleAddressType.RandomStatic, (UInt48)0xD9A06403396C);
        IBleDevice device = await GetMockDeviceAsync(async device => await device.SetRandomAddressAsync(address));
        IObservable<IGattServerPeer> observable = device.Central.ConnectToPeripheral(
            address,
            new BleConnectionParameters { ConnectionInterval = connectionInterval }
        );

        FluentTestObserver<IGattServerPeer> testObserver = observable.Observe();

        if (expectedResult)
        {
            await testObserver.Should().PushAsync(1);
        }
        else
        {
            await testObserver.Should().ThrowAsync<BleCentralConnectionFailedException>();
        }
    }

    [Theory]
    [InlineData((ScanTiming)3, ScanTiming.MinValue, false)]
    [InlineData(ScanTiming.MinValue, (ScanTiming)3, false)]
    [InlineData(ScanTiming.MinValue, ScanTiming.MinValue, true)]
    public async Task ConnectToPeripheral_BleScanParameters_WithDifferentParameters(
        ScanTiming scanInterval,
        ScanTiming scanWindow,
        bool expectedResult
    )
    {
        var address = new BleAddress(BleAddressType.RandomStatic, (UInt48)0xD9A06403396C);
        IBleDevice device = await GetMockDeviceAsync(async device => await device.SetRandomAddressAsync(address));
        IObservable<IGattServerPeer> observable = device.Central.ConnectToPeripheral(
            address,
            scanParameters: new BleObservationParameters() { ScanInterval = scanInterval, ScanWindow = scanWindow }
        );

        var testObserver = observable.Observe();

        if (expectedResult)
        {
            await testObserver.Should().PushAsync(1);
        }
        else
        {
            await testObserver.Should().ThrowAsync<BleCentralConnectionFailedException>();
        }
    }

    [Fact]
    public async Task ConnectToPeripheral_ShouldWork()
    {
        byte[] dataToWrite = [0x01, 0x02, 0x03, 0x04];
        var address = new BleAddress(BleAddressType.RandomStatic, (UInt48)0xD9A06403396C);
        IBleDevice device = await GetMockDeviceAsync(async d =>
        {
            await d.SetRandomAddressAsync(address);
            IGattClientService service = d.Peripheral.AddService(0xABCD);
            var notifyChar = service.AddCharacteristic<Properties.Notify>(0x1234);
            var writeChar = service.AddCharacteristic<Properties.Write>(
                0x5678,
                onWrite: async (peer, bytes) =>
                {
                    await notifyChar.NotifyAsync(peer, bytes);
                    return GattProtocolStatus.Success;
                }
            );
        });

        IGattServerPeer serverPeer = await device.Central.ConnectToPeripheral(address).FirstAsync();
        IGattServerService service = await serverPeer.DiscoverServiceAsync(0xABCD);
        var notifyChar = await service.DiscoverCharacteristicAsync<Properties.Notify>(0x1234);
        var writeChar = await service.DiscoverCharacteristicAsync<Properties.Write>(0x5678);

        await using IDisposableObservable<byte[]> notifyObservable = await notifyChar.OnNotifyAsync();
        Task<byte[]> notifyTask = notifyObservable.FirstAsync().ToTask();
        await writeChar.WriteAsync(dataToWrite);
        byte[] result = await notifyTask;

        result.Should().BeEquivalentTo(dataToWrite);
    }
}
