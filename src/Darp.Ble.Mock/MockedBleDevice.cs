using System.Reactive.Concurrency;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockedBleDevice(
    string? name,
    BleMockFactory.InitializeAsync onInitialize,
    IScheduler scheduler,
    ILoggerFactory loggerFactory
) : BleDevice(loggerFactory, loggerFactory.CreateLogger<MockedBleDevice>())
{
    private readonly BleMockFactory.InitializeAsync _onInitialize = onInitialize;
    public override string Identifier => BleDeviceIdentifiers.MockDevice;
    public override string? Name { get; } = name;
    public override AppearanceValues Appearance => AppearanceValues.Unknown;
    public IScheduler Scheduler { get; } = scheduler;

    public MockDeviceSettings Settings { get; } = new();

    public new MockedBlePeripheral Peripheral
    {
        get => (MockedBlePeripheral)base.Peripheral;
        set => base.Peripheral = value;
    }
    public new MockedBleBroadcaster Broadcaster
    {
        get => (MockedBleBroadcaster)base.Broadcaster;
        set => base.Broadcaster = value;
    }

    public BleAddress RandomAddress { get; private set; } = BleAddress.NewRandomStaticAddress();

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        RandomAddress = randomAddress;
        return Task.CompletedTask;
    }

    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Broadcaster = new MockedBleBroadcaster(this, LoggerFactory.CreateLogger<MockedBleBroadcaster>());
        Peripheral = new MockedBlePeripheral(this, LoggerFactory.CreateLogger<MockedBlePeripheral>());
        Peripheral.AddGapService();
        await _onInitialize(this, Settings).ConfigureAwait(false);
        return InitializeResult.Success;
    }

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        return Broadcaster.GetAdvertisements(observer);
    }
}
