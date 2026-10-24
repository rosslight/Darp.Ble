using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Scanning;

/// <summary> A running scan in a test. Started with one warm-up report, so it is registered and idle afterwards </summary>
internal sealed class ScanSession(IAsyncEnumerator<IGapAdvertisement> enumerator) : IAsyncDisposable
{
    private readonly IAsyncEnumerator<IGapAdvertisement> _enumerator = enumerator;

    public static async Task<ScanSession> StartAsync(
        IAsyncEnumerable<IGapAdvertisement> scan,
        TestObserverDriver driver,
        IGapAdvertisement warmUp
    )
    {
        var session = new ScanSession(scan.GetAsyncEnumerator(TestContext.Current.CancellationToken));
        // The first MoveNextAsync registers the scan; the warm-up report completes it
        ValueTask<bool> first = session._enumerator.MoveNextAsync();
        driver.Report(warmUp);
        if (!await first)
            throw new InvalidOperationException("The scan ended before the warm-up report");
        return session;
    }

    public async Task<IGapAdvertisement[]> TakeAsync(int count)
    {
        var advertisements = new IGapAdvertisement[count];
        for (var index = 0; index < count; index++)
        {
            if (!await _enumerator.MoveNextAsync())
                throw new InvalidOperationException($"The scan ended after {index} advertisements");
            advertisements[index] = _enumerator.Current;
        }
        return advertisements;
    }

    public async Task<bool> MoveNextAsync() => await _enumerator.MoveNextAsync();

    public ValueTask DisposeAsync() => _enumerator.DisposeAsync();
}
