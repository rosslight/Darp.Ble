using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Advertising;

/// <summary> A broadcaster driver whose sets record every call and fail the operations a test names </summary>
internal sealed class TestBroadcasterDriver : IBroadcasterDriver
{
    public List<TestAdvertisingSetDriver> Sets { get; } = [];

    /// <summary> Operations ("start", "stop", "parameters", "data", ...) that fail from now on </summary>
    public HashSet<string> Failing { get; } = [];

    public Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken
    )
    {
        var set = new TestAdvertisingSetDriver(this);
        Sets.Add(set);
        return Task.FromResult<IAdvertisingSetDriver>(set);
    }
}
