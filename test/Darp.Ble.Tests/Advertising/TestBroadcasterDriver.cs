using Darp.Ble.Backends;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Advertising;

/// <summary> A broadcaster driver whose sets record every call and fail the operations a test names </summary>
internal sealed class TestBroadcasterDriver : IBroadcasterDriver
{
    public List<TestAdvertisingSetDriver> Sets { get; } = [];

    /// <summary> Operations ("start", "stop", "parameters", "data", ...) that fail from now on </summary>
    public HashSet<string> Failing { get; } = [];

    /// <summary> When set, creating a set waits until the test completes it </summary>
    public TaskCompletionSource? HoldCreation { get; set; }

    public async Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    )
    {
        if (HoldCreation is not null)
            await HoldCreation.Task.ConfigureAwait(false);
        var set = new TestAdvertisingSetDriver(this);
        Sets.Add(set);
        return set;
    }
}
