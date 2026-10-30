using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary>
/// Work that simulated peripherals keep running while the mock adapter is open, e.g. a sensor that notifies.
/// The mock adapter owns it: closing the adapter cancels the work and waits for it before the peripherals are disposed
/// </summary>
public sealed class MockBackgroundWork
{
    private readonly ILogger _logger;
    private readonly CancellationToken _closing;
    private readonly Lock _lock = new();
    private readonly List<Task> _running = [];
    private bool _closed;

    internal MockBackgroundWork(TimeProvider timeProvider, ILogger logger, CancellationToken closing)
    {
        TimeProvider = timeProvider;
        _logger = logger;
        _closing = closing;
    }

    /// <summary> The manager's clock; timers in the work use it, so a test's fake clock drives them like the library's own </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary> Starts work that runs until the mock adapter closes </summary>
    /// <param name="work"> The work; its token is cancelled when the mock adapter closes. A failure is logged and
    /// thrown again when the mock adapter is disposed </param>
    /// <exception cref="ObjectDisposedException"> The mock adapter is closed </exception>
    public void Run(Func<CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            // Off the caller's stack, so a peripheral's setup is not blocked by the work it starts
            _running.Add(Task.Run(() => RunAsync(work)));
        }
    }

    private async Task RunAsync(Func<CancellationToken, Task> work)
    {
        try
        {
            await work(_closing).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
        catch (Exception e)
        {
            // Logged now, since nobody sees it before the mock adapter is disposed
            _logger.LogBackgroundWorkFailed(e);
            throw;
        }
    }

    /// <summary> Takes no new work and waits for the running work, which the closing token ends; returns what failed </summary>
    internal async Task<IReadOnlyList<Exception>> WhenStoppedAsync()
    {
        Task[] running;
        lock (_lock)
        {
            _closed = true;
            running = [.. _running];
        }
        List<Exception> failures = [];
        foreach (Task task in running)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
        }
        return failures;
    }
}
