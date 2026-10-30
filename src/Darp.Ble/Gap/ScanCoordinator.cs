using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> Shares one radio scan between all scans of an observer (D6) </summary>
/// <remarks>
/// <see cref="_lock"/> guards the listeners, the current radio run and admission; it is held only for those fields.
/// <see cref="_commands"/> serializes the asynchronous driver commands (start, stop, restart) and owns
/// <see cref="_radioMayScan"/>: once a start was attempted, the radio counts as scanning until a stop succeeded
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The semaphore outlives disposal: scans that end afterwards still wait on it. It holds no wait handle"
)]
internal sealed class ScanCoordinator(IObserverDriver driver, ILogger logger)
{
    private readonly IObserverDriver _driver = driver;
    private readonly ILogger _logger = logger;
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _commands = new(1, 1);
    private ScanListener[] _listeners = [];
    private RadioRun? _run;
    private bool _closed;
    private bool _radioMayScan;

    public BleScanParameters Parameters { get; private set; } = new();

    public ScanDropCounter DropCounter { get; } = new();

    public async Task<ScanListener> AddListenerAsync(QueueOptions queue, CancellationToken cancellationToken)
    {
        var listener = new ScanListener(queue, DropCounter);
        await _commands.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RadioRun? runToStart = null;
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_closed, typeof(BleObserver));
                _listeners = [.. _listeners, listener];
                if (_run is null)
                {
                    runToStart = new RadioRun(this);
                    _run = runToStart;
                }
            }
            if (runToStart is not null)
                await StartRunAsync(runToStart, listener, cancellationToken).ConfigureAwait(false);
            return listener;
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task RemoveListenerAsync(ScanListener listener)
    {
        listener.Complete();
        await _commands.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            bool stop;
            lock (_lock)
            {
                _listeners = Without(_listeners, listener);
                stop = _listeners.Length == 0 && _run is not null;
                if (stop)
                    _run = null;
            }
            if (stop)
                await StopRadioAsync().ConfigureAwait(false);
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task ConfigureAsync(BleScanParameters parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate(nameof(parameters));
        await _commands.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            BleScanParameters previous = Parameters;
            RadioRun? oldRun;
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_closed, typeof(BleObserver));
                oldRun = _run;
            }
            Parameters = parameters;
            if (oldRun is null)
                return;

            _logger.LogScanReconfigured(previous, parameters);
            var newRun = new RadioRun(this);
            try
            {
                await StopRadioAsync().ConfigureAwait(false);
                bool restart;
                lock (_lock)
                {
                    // If the old run failed meanwhile, its scans already ended and nobody needs the radio
                    restart = ReferenceEquals(_run, oldRun);
                    // Reports and failures of the old run are ignored from here on
                    if (restart)
                        _run = newRun;
                }
                if (!restart)
                    return;
                _radioMayScan = true;
                await _driver.StartScanAsync(parameters, newRun, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // The scans cannot continue without the radio. Whichever run is current fails
                Fail(oldRun, e);
                Fail(newRun, e);
                throw;
            }
        }
        finally
        {
            _commands.Release();
        }
    }

    /// <summary> Closes admission so no scan can start any more </summary>
    public void CloseAdmission()
    {
        lock (_lock)
            _closed = true;
    }

    /// <summary> Closes admission, ends every scan normally and stops the radio </summary>
    public async Task DisposeAsync()
    {
        CloseAdmission();
        await _commands.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            ScanListener[] listeners;
            lock (_lock)
            {
                listeners = _listeners;
                _listeners = [];
                _run = null;
            }
            foreach (ScanListener listener in listeners)
                listener.Complete();
            await StopRadioAsync().ConfigureAwait(false);
        }
        finally
        {
            _commands.Release();
        }
    }

    private async Task StartRunAsync(RadioRun run, ScanListener listener, CancellationToken cancellationToken)
    {
        try
        {
            // A stop that failed earlier is retried first, so the driver never starts twice
            await StopRadioAsync().ConfigureAwait(false);
            _radioMayScan = true;
            await _driver.StartScanAsync(Parameters, run, cancellationToken).ConfigureAwait(false);
            _logger.LogScanStarted(Parameters.ScanType);
        }
        catch
        {
            lock (_lock)
            {
                if (ReferenceEquals(_run, run))
                    _run = null;
                _listeners = Without(_listeners, listener);
            }
            throw;
        }
    }

    /// <summary> Stops the radio if it may still scan; only called under <see cref="_commands"/> </summary>
    private async Task StopRadioAsync()
    {
        if (!_radioMayScan)
            return;
        await _driver.StopScanAsync(CancellationToken.None).ConfigureAwait(false);
        _radioMayScan = false;
        _logger.LogScanStopped();
    }

    private void Deliver(RadioRun run, IGapAdvertisement advertisement)
    {
        ScanListener[] listeners;
        lock (_lock)
        {
            if (!ReferenceEquals(_run, run))
                return;
            listeners = _listeners;
        }
        foreach (ScanListener listener in listeners)
        {
            if (listener.TryWrite(advertisement))
                continue;
            lock (_lock)
                _listeners = Without(_listeners, listener);
        }
    }

    private void Fail(RadioRun run, Exception error)
    {
        ScanListener[] listeners;
        lock (_lock)
        {
            if (!ReferenceEquals(_run, run))
                return;
            _run = null;
            listeners = _listeners;
            _listeners = [];
        }
        foreach (ScanListener listener in listeners)
            listener.Complete(error);
    }

    private static ScanListener[] Without(ScanListener[] listeners, ScanListener listener) =>
        Array.IndexOf(listeners, listener) < 0 ? listeners : [.. listeners.Where(l => !ReferenceEquals(l, listener))];

    /// <summary> The sink of one radio run. Calls of a run that was stopped or replaced are ignored </summary>
    private sealed class RadioRun(ScanCoordinator coordinator) : IScanSink
    {
        private readonly ScanCoordinator _coordinator = coordinator;

        public void OnReport(IGapAdvertisement advertisement) => _coordinator.Deliver(this, advertisement);

        public void OnFailed(BleException error) => _coordinator.Fail(this, error);
    }
}
