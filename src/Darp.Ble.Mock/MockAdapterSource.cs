using Darp.Ble.Backends;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterSource(string name, BleOpenErrorKind? openFailure) : IBleAdapterSource
{
    private readonly BleOpenErrorKind? _openFailure = openFailure;

    public string? Name { get; } = name;

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IBleAdapterDriver>(cancellationToken);
        if (_openFailure is { } kind)
        {
            return Task.FromException<IBleAdapterDriver>(
                new BleOpenException(kind, $"The mock adapter '{Name}' is configured to fail opening with {kind}")
            );
        }
        return Task.FromResult<IBleAdapterDriver>(new MockAdapterDriver());
    }
}
