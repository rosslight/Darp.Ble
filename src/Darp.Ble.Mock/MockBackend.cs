using Darp.Ble.Backends;

namespace Darp.Ble.Mock;

internal sealed class MockBackend(string adapterName) : IBleBackend
{
    private readonly IBleAdapterSource[] _adapters = [new MockAdapterSource(adapterName)];

    public string Name => BleBackends.Mock;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    ) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<IReadOnlyList<IBleAdapterSource>>(cancellationToken)
            : Task.FromResult<IReadOnlyList<IBleAdapterSource>>(_adapters);
}
