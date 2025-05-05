using System.Reactive.Concurrency;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

internal sealed class MockedBleDevice(
    string? name,
    BleMockFactory.InitializeAsync onInitialize,
    IScheduler scheduler,
    IServiceProvider serviceProvider
) : BleDevice(serviceProvider, serviceProvider.GetLogger<MockedBleDevice>())
{
    private readonly BleMockFactory.InitializeAsync _onInitialize = onInitialize;
    private BleAddress _randomAddress = BleAddress.NewRandomStaticAddress();

    public override string Identifier => BleDeviceIdentifiers.MockDevice;
    public override string? Name { get; set; } = name;
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

    public override BleAddress RandomAddress => _randomAddress;

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        _randomAddress = randomAddress;
        return Task.CompletedTask;
    }

    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Broadcaster = new MockedBleBroadcaster(this, ServiceProvider.GetLogger<MockedBleBroadcaster>());
        Peripheral = new MockedBlePeripheral(this, ServiceProvider.GetLogger<MockedBlePeripheral>());
        Peripheral.AddGapService();
        await _onInitialize(this, Settings).ConfigureAwait(false);
        return InitializeResult.Success;
    }

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        return Broadcaster.GetAdvertisements(observer);
    }
}
