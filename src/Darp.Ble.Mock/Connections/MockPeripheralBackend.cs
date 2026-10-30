using Darp.Ble.Backends;

namespace Darp.Ble.Mock.Connections;

/// <summary> Opens one simulated peripheral in the air of its mock adapter; peripherals have no peripherals of their own </summary>
internal sealed class MockPeripheralBackend(MockAir air, string name) : IBleBackend, IBleAdapterSource
{
    private readonly MockAir _air = air;

    public string Name => BleBackends.Mock;

    string? IBleAdapterSource.Name { get; } = name;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlyList<IBleAdapterSource>>([this]);

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken) =>
        Task.FromResult<IBleAdapterDriver>(new MockAdapterDriver(_air, context.LoggerFactory));
}
