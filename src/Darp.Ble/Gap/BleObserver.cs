using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> The observer role of an adapter: scans for advertisements </summary>
/// <remarks> All scans share one radio scan, which runs while at least one scan is enumerated </remarks>
public sealed class BleObserver
{
    private readonly ScanCoordinator _coordinator;
    private readonly QueueOptions _defaultQueue;
    private readonly ILogger _logger;

    internal BleObserver(BleAdapter adapter, IObserverDriver driver, QueueOptions defaultQueue, ILogger logger)
    {
        Adapter = adapter;
        _coordinator = new ScanCoordinator(driver, logger);
        _defaultQueue = defaultQueue;
        _logger = logger;
    }

    /// <summary> The adapter this observer belongs to </summary>
    public BleAdapter Adapter { get; }

    /// <summary> The radio settings of all scans </summary>
    public BleScanParameters Parameters => _coordinator.Parameters;

    internal long DroppedAdvertisementCount => _coordinator.DropCounter.Count;

    /// <summary> Changes the radio settings of all scans </summary>
    /// <remarks> While scans run, the radio scan restarts with the new settings; the scans keep running and see a short gap </remarks>
    /// <param name="parameters"> The new radio settings </param>
    /// <param name="cancellationToken"> Cancels waiting for other scan commands </param>
    /// <returns> A task that completes once the settings apply </returns>
    /// <exception cref="ArgumentException"> The interval or window is not positive, or the window is longer than the interval </exception>
    public Task ConfigureAsync(BleScanParameters parameters, CancellationToken cancellationToken = default) =>
        _coordinator.ConfigureAsync(parameters, cancellationToken);

    /// <summary> Scans while enumerated, with the default queue of the <see cref="BleManager"/> </summary>
    /// <param name="cancellationToken"> Ends the scan </param>
    /// <returns> The advertisements, in the order they were received </returns>
    /// <exception cref="BleException"> The radio stopped scanning; thrown after the queued advertisements </exception>
    [SuppressMessage(
        "ApiDesign",
        "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads",
        Justification = "The common call passes only a token; the overload with a queue takes it as a required parameter"
    )]
    public IAsyncEnumerable<IGapAdvertisement> ScanAsync(CancellationToken cancellationToken = default) =>
        ScanAsync(_defaultQueue, cancellationToken);

    /// <summary> Scans while enumerated. Each call has its own queue; ending the loop ends this scan </summary>
    /// <param name="queue"> The queue of this scan </param>
    /// <param name="cancellationToken"> Ends the scan </param>
    /// <returns> The advertisements, in the order they were received </returns>
    /// <exception cref="BleQueueOverflowException"> The queue was full and has <see cref="QueueFullMode.Fail"/> </exception>
    /// <exception cref="BleException"> The radio stopped scanning; thrown after the queued advertisements </exception>
    public async IAsyncEnumerable<IGapAdvertisement> ScanAsync(
        QueueOptions queue,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(queue);
        ScanListener listener = await _coordinator.AddListenerAsync(queue, cancellationToken).ConfigureAwait(false);
        try
        {
            while (await WaitForAdvertisementsAsync(listener, cancellationToken).ConfigureAwait(false))
            {
                while (listener.Reader.TryRead(out IGapAdvertisement? advertisement))
                {
                    // A queue that never runs empty must not delay cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return advertisement;
                }
            }
        }
        finally
        {
            await _coordinator.RemoveListenerAsync(listener).ConfigureAwait(false);
        }
    }

    internal void CloseAdmission() => _coordinator.CloseAdmission();

    internal Task DisposeAsync() => _coordinator.DisposeAsync();

    /// <summary> Stops the radio scan while the adapter connects; scans stay registered </summary>
    internal Task PauseAsync(CancellationToken cancellationToken) => _coordinator.PauseAsync(cancellationToken);

    /// <summary> Ends one pause; never throws, failures end the scans </summary>
    internal Task ResumeAsync() => _coordinator.ResumeAsync();

    private async Task<bool> WaitForAdvertisementsAsync(ScanListener listener, CancellationToken cancellationToken)
    {
        try
        {
            return await listener.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (BleQueueOverflowException e)
        {
            // Logged here, in the app's loop, instead of on the driver's read loop
            _logger.LogScanQueueOverflow(e.Capacity);
            throw;
        }
        catch (BleException e)
        {
            _logger.LogScanFailed(e);
            throw;
        }
    }
}
