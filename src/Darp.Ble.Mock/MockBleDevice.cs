using System.Reactive.Concurrency;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock implementation of a ble device </summary>
internal sealed class MockBleDevice(
    IReadOnlyList<(BleMockFactory.InitializeAsync OnInitialize, string? Name)> deviceConfigurations,
    string name,
    IScheduler scheduler,
    IServiceProvider serviceProvider
) : BleDevice(serviceProvider, serviceProvider.GetLogger<MockBleDevice>())
{
    private readonly IReadOnlyList<(BleMockFactory.InitializeAsync OnInitialize, string? Name)> _deviceConfigurations =
        deviceConfigurations;
    private readonly List<MockedBleDevice> _mockedDevices = [];
    private BleAddress _randomAddress = BleAddress.NewRandomStaticAddress();

    public IReadOnlyCollection<MockedBleDevice> MockedDevices => _mockedDevices;

    /// <inheritdoc />
    public override string? Name { get; set; } = name;

    /// <inheritdoc />
    public override AppearanceValues Appearance { get; set; } = AppearanceValues.Unknown;

    public IScheduler Scheduler { get; } = scheduler;

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.Mock;

    /// <inheritdoc />
    public override BleAddress RandomAddress => _randomAddress;

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        _randomAddress = randomAddress;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        foreach ((BleMockFactory.InitializeAsync onInitialize, string? peripheralName) in _deviceConfigurations)
        {
            var device = new MockedBleDevice(peripheralName, onInitialize, Scheduler, ServiceProvider);
            await device.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _mockedDevices.Add(device);
        }

        Observer = new MockBleObserver(this, ServiceProvider.GetLogger<MockBleObserver>());
        Central = new MockBleCentral(this, ServiceProvider.GetLogger<MockBleCentral>());
        return InitializeResult.Success;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (MockedBleDevice mockedBleDevice in _mockedDevices)
        {
            await mockedBleDevice.DisposeAsync().ConfigureAwait(false);
        }
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
