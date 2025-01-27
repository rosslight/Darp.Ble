using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock implementation of a ble device </summary>
internal sealed class MockBleDevice(
    IReadOnlyList<(BleMockFactory.InitializeAsync OnInitialize, string? Name)> peripheralConfigurations,
    string name,
    IScheduler scheduler,
    ILoggerFactory loggerFactory) : BleDevice(loggerFactory, loggerFactory.CreateLogger<MockBleDevice>())
{
    private readonly IReadOnlyList<(BleMockFactory.InitializeAsync OnInitialize, string? Name)> _peripheralConfigurations = peripheralConfigurations;
    private readonly List<MockedBlePeripheralDevice> _mockedPeripherals = [];
    public IReadOnlyCollection<MockedBlePeripheralDevice> MockedPeripherals => _mockedPeripherals;

    /// <inheritdoc />
    public override string Name { get; } = name;

    public IScheduler Scheduler { get; } = scheduler;

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.Mock;

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        foreach ((BleMockFactory.InitializeAsync onInitialize, string? peripheralName) in _peripheralConfigurations)
        {
            var device = new MockedBlePeripheralDevice(peripheralName, Scheduler, LoggerFactory);
            await device.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await onInitialize.Invoke(device).ConfigureAwait(false);
            _mockedPeripherals.Add(device);
        }

        Observer = new MockBleObserver(this, LoggerFactory.CreateLogger<MockBleObserver>());
        //Central = new MockBleCentral(this, Logger);
        return InitializeResult.Success;
    }
}

internal sealed class MockedBlePeripheralDevice(string? name, IScheduler scheduler, ILoggerFactory loggerFactory)
    : BleDevice(loggerFactory, loggerFactory.CreateLogger<MockedBlePeripheralDevice>())
{
    public override string Identifier => "Darp.Ble.Mock.Peripheral";
    public override string? Name { get; } = name;
    public IScheduler Scheduler { get; } = scheduler;

    public BleAddress RandomAddress { get; private set; } = BleAddress.NewRandomStaticAddress();

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        RandomAddress = randomAddress;
        return Task.CompletedTask;
    }

    protected override Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Broadcaster = new MockBleBroadcaster(this, LoggerFactory.CreateLogger<MockBleBroadcaster>());
        // var peripheral = new MockBlePeripheral(this, broadcaster, Logger);
        return Task.FromResult(InitializeResult.Success);
    }

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        if (Broadcaster is not MockBleBroadcaster mockBleBroadcaster)
        {
            return Observable.Throw<IGapAdvertisement>(new Exception("Broadcaster has to be a mock broadcaster"));
        }
        return mockBleBroadcaster.GetAdvertisements(observer);
    }
}