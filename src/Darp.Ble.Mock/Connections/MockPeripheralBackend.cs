using Darp.Ble.Backends;

namespace Darp.Ble.Mock.Connections;

/// <summary> Opens one simulated peripheral in the air of its mock adapter; peripherals have no peripherals of their own </summary>
internal sealed class MockPeripheralBackend(MockAir air, MockPeripheral peripheral) : IBleBackend, IBleAdapterSource
{
    private readonly MockAir _air = air;
    private readonly MockPeripheral _peripheral = peripheral;

    public string Name => BleBackends.Mock;

    string? IBleAdapterSource.Name => _peripheral.Name;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlyList<IBleAdapterSource>>([this]);

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken) =>
        Task.FromResult<IBleAdapterDriver>(
            // Each open of the mock adapter opens its peripherals once, through a backend of their own
            new MockAdapterDriver(
                _air,
                _peripheral.IncludeGattService,
                _peripheral.Range,
                closed: () => { },
                context.LoggerFactory
            )
        );
}
