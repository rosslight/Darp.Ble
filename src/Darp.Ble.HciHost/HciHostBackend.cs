using Darp.Ble.Backends;

namespace Darp.Ble.HciHost;

/// <summary> The HCI backend; finds its adapters anew on every enumeration, e.g. dongles plugged in since </summary>
internal sealed class HciHostBackend(Func<IReadOnlyList<IBleAdapterSource>> enumerate) : IBleBackend
{
    private readonly Func<IReadOnlyList<IBleAdapterSource>> _enumerate = enumerate;

    public string Name => BleBackends.HciHost;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    ) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<IReadOnlyList<IBleAdapterSource>>(cancellationToken)
            : Task.FromResult(_enumerate());
}
