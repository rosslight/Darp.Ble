using System.Diagnostics.CodeAnalysis;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleDeviceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        var loggerFactory = new TestLoggerFactory();
        BleManager manager = new BleManagerBuilder().SetLogger(loggerFactory).AddMock().CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync(Token);
        loggerFactory
            .GetLogger<MockBleDevice>()
            .LogEntries.ShouldBe([(LogLevel.Debug, $"Ble device '{device.Name}' initialized!")]);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_FailedInitialization_ShouldHaveCorrectResult()
    {
        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(InitializeResult.DeviceNotAvailable));

        InitializeResult result = await device.InitializeAsync(Token);

        result.ShouldBe(InitializeResult.DeviceNotAvailable);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitializing()
    {
        var testScheduler = new TestScheduler();

        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ =>
                Observable
                    .Return(InitializeResult.Success)
                    .Delay(TimeSpan.FromMilliseconds(1000), testScheduler)
                    .ToTask()
            );

        Task<InitializeResult> init1Task = device.InitializeAsync(Token);
        Task<InitializeResult> init2Task = device.InitializeAsync(Token);

        testScheduler.AdvanceTo(TimeSpan.FromMilliseconds(1001).Ticks);

        (await init1Task).ShouldBe(InitializeResult.Success);
        (await init2Task).ShouldBe(InitializeResult.AlreadyInitializing);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitialized()
    {
        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        device.IsInitialized.ShouldBeFalse();
        InitializeResult init1 = await device.InitializeAsync(Token);
        init1.ShouldBe(InitializeResult.Success);

        device.IsInitialized.ShouldBeTrue();
        InitializeResult init2 = await device.InitializeAsync(Token);
        init2.ShouldBe(InitializeResult.Success);
    }

    [Fact]
    public void Capability_NotInitialized_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);
        Action act = () => _ = device.Observer;
        act.ShouldThrow<NotInitializedException>();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task Capability_NotSupported_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        await device.InitializeAsync(Token);

        Action act = () => _ = device.Observer;
        act.ShouldThrow<NotSupportedException>();
    }

    [Fact]
    [SuppressMessage("Usage", "NS5000:Received check.")]
    public async Task DisposeAsync()
    {
        var device = Substitute.For<BleDevice>(NullServiceProvider.Instance, NullLogger<BleDevice>.Instance);

        await device.DisposeAsync();

        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("DisposeAsyncCore");
        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("Dispose", true);
    }

    [Fact]
    public async Task ConnectingAndDisposing_ShouldNotThrow()
    {
        var device = (MockBleDevice)
            new BleMockFactory()
                .AddPeripheral(async d => await d.Broadcaster.StartAdvertisingAsync(interval: ScanTiming.Ms1000))
                .EnumerateDevices(NullServiceProvider.Instance)
                .First();
        await device.InitializeAsync(Token);

        IObservable<IGapAdvertisement> advObservable = device.Observer.OnAdvertisement();
        await device.Observer.StartObservingAsync(Token);
        IGattServerPeer peer = await advObservable.ConnectToPeripheral().FirstAsync();
        await peer.DisposeAsync();
    }

    /*
[Fact]
    public async Task Asdsadasd()
    {
        byte[] bytes = Convert.FromHexString("1234");

        var device = (MockBleDevice)new BleMockFactory
        {
            OnInitialize = async (broadcaster, peripheral) =>
            {
                IGattClientService service = await peripheral.AddServiceAsync(0x1234);
                IGattClientCharacteristic<Properties.Write> characteristic =
                    await service.AddCharacteristicAsync<Properties.Write>(0x1235);
                IGattClientCharacteristic<Properties.Notify> notify =
                    await service.AddCharacteristicAsync<Properties.Notify>(0x1236);
                characteristic.OnWrite(async (peer, received, token) =>
                {
                    await notify.NotifyAsync(peer, received, token);
                    return GattProtocolStatus.Success;
                });
                broadcaster.Advertise(Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Select(_ => AdvertisingData.From([
                        (AdTypes.Flags,
                        [
                            (byte)(AdvertisingDataFlags.ClassicNotSupported | AdvertisingDataFlags.GeneralDiscoverableMode),
                        ]),
                        (AdTypes.IncompleteListOf16BitServiceClassUuids, [0x4c, 0xfd]),
                    ])), new AdvertisingParameters {Type = BleEventType.Connectable});
            },
        }.EnumerateDevices(logger: null).First();
        await device.InitializeAsync();
        var peer = await device.Observer.RefCount().ConnectToPeripheral().FirstAsync();
        var service = await peer.DiscoverServiceAsync(new BleUuid(0x1234));
        var writeChar = await service.DiscoverCharacteristicAsync<Properties.Write>(new BleUuid(0x1235));
        var notifyChar = await service.DiscoverCharacteristicAsync<Properties.Notify>(new BleUuid(0x1236));
        //var disposable = await notifyChar.OnNotifyAsync();
        await peer.DisposeAsync();
        //await disposable.DisposeAsync();
    }
     */
}
