using Darp.Ble.Backends;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Scanning;

/// <summary> An observer driver that records the radio commands and lets the test push reports </summary>
internal sealed class TestObserverDriver : IObserverDriver
{
    private IScanSink? _sink;

    public List<BleScanParameters> Starts { get; } = [];

    public int StopCount { get; private set; }

    public bool IsScanning { get; private set; }

    public Exception? StartFailure { get; set; }

    public Exception? StopFailure { get; set; }

    /// <summary> When set, stopping waits until the test completes it </summary>
    public TaskCompletionSource? HoldStop { get; set; }

    /// <summary> The sink of the current or the last run </summary>
    public IScanSink Sink => _sink ?? throw new InvalidOperationException("The radio never scanned");

    public Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken)
    {
        if (StartFailure is not null)
            return Task.FromException(StartFailure);
        Starts.Add(parameters);
        _sink = sink;
        IsScanning = true;
        return Task.CompletedTask;
    }

    public async Task StopScanAsync(CancellationToken cancellationToken)
    {
        StopCount++;
        if (HoldStop is not null)
            await HoldStop.Task.ConfigureAwait(false);
        if (StopFailure is not null)
            throw StopFailure;
        IsScanning = false;
    }

    public void Report(params IGapAdvertisement[] advertisements)
    {
        foreach (IGapAdvertisement advertisement in advertisements)
            Sink.OnReport(advertisement);
    }

    public void Fail(BleException error) => Sink.OnFailed(error);
}
