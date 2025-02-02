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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Reactive.Testing;
using NSubstitute;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleDeviceTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        var logger = new TestLogger();
        BleManager manager = new BleManagerBuilder()
            .SetLogger(new TestLoggerFactory(logger))
            .Add<BleMockFactory>()
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        logger.LogEntries.Should().BeEquivalentTo([(LogLevel.Debug, $"Ble device '{device.Name}' initialized!")]);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_FailedInitialization_ShouldHaveCorrectResult()
    {
        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(InitializeResult.DeviceNotAvailable));

        InitializeResult result = await device.InitializeAsync();

        result.Should().Be(InitializeResult.DeviceNotAvailable);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitializing()
    {
        var testScheduler = new TestScheduler();

        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ =>
                Observable
                    .Return(InitializeResult.Success)
                    .Delay(TimeSpan.FromMilliseconds(1000), testScheduler)
                    .ToTask()
            );

        Task<InitializeResult> init1Task = device.InitializeAsync();
        Task<InitializeResult> init2Task = device.InitializeAsync();

        testScheduler.AdvanceTo(TimeSpan.FromMilliseconds(1001).Ticks);

        (await init1Task).Should().Be(InitializeResult.Success);
        (await init2Task).Should().Be(InitializeResult.AlreadyInitializing);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitialized()
    {
        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        device.IsInitialized.Should().BeFalse();
        InitializeResult init1 = await device.InitializeAsync();
        init1.Should().Be(InitializeResult.Success);

        device.IsInitialized.Should().BeTrue();
        InitializeResult init2 = await device.InitializeAsync();
        init2.Should().Be(InitializeResult.Success);
    }

    [Fact]
    public void Capability_NotInitialized_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);
        Action act = () => _ = device.Observer;
        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task Capability_NotSupported_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);
        device
            .InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        await device.InitializeAsync();

        Action act = () => _ = device.Observer;
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    [SuppressMessage("Usage", "NS5000:Received check.")]
    public async Task DisposeAsync()
    {
        var device = Substitute.For<BleDevice>(NullLoggerFactory.Instance, NullLogger<BleDevice>.Instance);

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
                .EnumerateDevices(NullLoggerFactory.Instance)
                .First();
        await device.InitializeAsync();
        IGattServerPeer peer = await device.Observer.RefCount().ConnectToPeripheral().FirstAsync();
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
