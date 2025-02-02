using System.Reactive.Concurrency;
using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock implementation of a ble device </summary>
internal sealed class MockBleDevice(
    IReadOnlyList<(BleMockFactory.InitializeAsync OnInitialize, string? Name)> deviceConfigurations,
    string name,
    IScheduler scheduler,
    ILoggerFactory loggerFactory
) : BleDevice(loggerFactory, loggerFactory.CreateLogger<MockBleDevice>())
{
    private readonly IReadOnlyList<(
        BleMockFactory.InitializeAsync OnInitialize,
        string? Name
    )> _deviceConfigurations = deviceConfigurations;
    private readonly List<MockedBleDevice> _mockedDevices = [];

    public IReadOnlyCollection<MockedBleDevice> MockedDevices => _mockedDevices;

    /// <inheritdoc />
    public override string Name { get; } = name;

    public IScheduler Scheduler { get; } = scheduler;

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.Mock;

    protected override Task SetRandomAddressAsyncCore(
        BleAddress randomAddress,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(
        CancellationToken cancellationToken
    )
    {
        foreach (
            (
                BleMockFactory.InitializeAsync onInitialize,
                string? peripheralName
            ) in _deviceConfigurations
        )
        {
            var device = new MockedBleDevice(
                peripheralName,
                onInitialize,
                Scheduler,
                LoggerFactory
            );
            await device.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _mockedDevices.Add(device);
        }

        Observer = new MockBleObserver(this, LoggerFactory.CreateLogger<MockBleObserver>());
        Central = new MockBleCentral(this, LoggerFactory.CreateLogger<MockBleCentral>());
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
