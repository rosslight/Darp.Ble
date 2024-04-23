using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock;
using FluentAssertions;
using FluentAssertions.Reactive;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleCentralTests
{
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
        return await GetMockDeviceAsync(Configure);

        async Task Configure(IBleBroadcaster broadcaster, IBlePeripheral peripheral)
        {
            IObservable<AdvertisingData> source = Observable.Interval(TimeSpan.FromMilliseconds(1000), scheduler)
                .Select(_ => AdvertisingData.Empty);
            await peripheral.AddServiceAsync(0x1234);
            broadcaster.Advertise(source);
        }
    }

    [Theory]
    [InlineData((ConnectionTiming)5, false)]
    [InlineData(ConnectionTiming.MinValue, true)]
    [InlineData(ConnectionTiming.MaxValue, true)]
    [InlineData((ConnectionTiming)3201, false)]
    public async Task ConnectToPeripheral_BleConnectionParameters_WithDifferentParameters(ConnectionTiming connectionInterval, bool expectedResult)
    {
        var address = new BleAddress((UInt48)0xAABBCCDDEEFF);
        IBleDevice central = await GetMockDeviceAsync((_, _) => Task.CompletedTask);
        IObservable<IGattServerPeer> observable = central.Central.ConnectToPeripheral(address, new BleConnectionParameters
        {
            ConnectionInterval = connectionInterval,
        });

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

    [Theory]
    [InlineData((ScanTiming)3, ScanTiming.MinValue, false)]
    [InlineData(ScanTiming.MinValue, (ScanTiming)3, false)]
    [InlineData(ScanTiming.MinValue, ScanTiming.MinValue, true)]
    public async Task ConnectToPeripheral_BleScanParameters_WithDifferentParameters(ScanTiming scanInterval, ScanTiming scanWindow, bool expectedResult)
    {
        var address = new BleAddress((UInt48)0xAABBCCDDEEFF);
        IBleDevice central = await GetMockDeviceAsync((_, _) => Task.CompletedTask);
        IObservable<IGattServerPeer> observable = central.Central.ConnectToPeripheral(address, scanParameters: new BleScanParameters()
        {
            ScanInterval = scanInterval,
            ScanWindow = scanWindow,
        });

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
}