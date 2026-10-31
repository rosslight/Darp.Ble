using System.Runtime.CompilerServices;

namespace Darp.Ble.Testing;

/// <summary>
/// Bounds a test's waits for signals in real time, so a regression fails the test with what it waited for instead of
/// hanging the run. Protocol time stays on the fake clocks; only a broken test ever waits this long
/// </summary>
internal static class Watchdog
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    /// <summary> Waits for the task, failing the test when it does not complete in time </summary>
    public static async Task WithWatchdog(this Task task, [CallerArgumentExpression(nameof(task))] string awaited = "")
    {
        try
        {
            await task.WaitAsync(Limit, TimeProvider.System, TestContext.Current.CancellationToken);
        }
        catch (TimeoutException e)
        {
            throw Expired(awaited, e);
        }
    }

    /// <summary> Waits for the task, failing the test when it does not complete in time </summary>
    public static async Task<T> WithWatchdog<T>(
        this Task<T> task,
        [CallerArgumentExpression(nameof(task))] string awaited = ""
    )
    {
        try
        {
            return await task.WaitAsync(Limit, TimeProvider.System, TestContext.Current.CancellationToken);
        }
        catch (TimeoutException e)
        {
            throw Expired(awaited, e);
        }
    }

    /// <summary> Polls until the condition holds, failing the test when it does not in time </summary>
    /// <param name="condition"> The condition </param>
    /// <param name="step"> Runs after each failed poll, e.g. to move a fake clock on </param>
    /// <param name="awaited"> What the test waits for, in the failure </param>
    public static async Task UntilAsync(
        Func<bool> condition,
        Action? step = null,
        [CallerArgumentExpression(nameof(condition))] string awaited = ""
    )
    {
        long started = TimeProvider.System.GetTimestamp();
        while (!condition())
        {
            if (TimeProvider.System.GetElapsedTime(started) > Limit)
                throw Expired(awaited, innerException: null);
            step?.Invoke();
            await Task.Delay(TimeSpan.FromMilliseconds(1), TimeProvider.System, TestContext.Current.CancellationToken);
        }
    }

    private static TimeoutException Expired(string awaited, Exception? innerException) =>
        new($"Waited {Limit.TotalSeconds:0} s for {awaited}", innerException);
}
