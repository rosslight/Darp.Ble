using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> A backend for what the mock cannot show: failing enumeration and the driver boundary </summary>
internal sealed class TestBackend(Func<IReadOnlyList<IBleAdapterSource>> enumerate) : IBleBackend
{
    private readonly Func<IReadOnlyList<IBleAdapterSource>> _enumerate = enumerate;

    public TestBackend(IBleAdapterDriver driver)
        : this(() => [new TestAdapterSource(driver)]) { }

    public string Name => "Test";

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    ) => Task.FromResult(_enumerate());
}
