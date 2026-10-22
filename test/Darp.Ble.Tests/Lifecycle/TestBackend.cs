using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> A backend that lists the adapters a test gives it </summary>
internal sealed class TestBackend(string name, params TestAdapterSource[] adapters) : IBleBackend
{
    private readonly TestAdapterSource[] _adapters = adapters;

    public string Name { get; } = name;

    public BleBackendContext? EnumerationContext { get; private set; }

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    )
    {
        EnumerationContext = context;
        return Task.FromResult<IReadOnlyList<IBleAdapterSource>>(_adapters);
    }
}
