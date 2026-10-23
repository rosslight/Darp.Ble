using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> An adapter that opens to a driver the test controls </summary>
internal sealed class TestAdapterSource(IBleAdapterDriver driver) : IBleAdapterSource
{
    private readonly IBleAdapterDriver _driver = driver;

    public string? Name => "Test adapter";

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken) =>
        Task.FromResult(_driver);
}
