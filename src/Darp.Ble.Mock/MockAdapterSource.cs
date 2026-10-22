using Darp.Ble.Backends;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterSource(string name) : IBleAdapterSource
{
    public string? Name { get; } = name;

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IBleAdapterDriver>(cancellationToken);
        return Task.FromResult<IBleAdapterDriver>(new MockAdapterDriver());
    }
}
