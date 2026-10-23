using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> A connection to a peripheral, from the central side. It ends once; afterwards it cannot be used again </summary>
public sealed class ConnectedPeripheral : IAsyncDisposable
{
    private readonly BleCentral _central;
    private readonly IConnectionDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private DisconnectInfo? _ending;

    internal ConnectedPeripheral(BleCentral central, BleAddress address, IConnectionDriver driver, ILogger logger)
    {
        _central = central;
        _driver = driver;
        _logger = logger;
        Address = address;
    }

    /// <summary> The address of the peripheral </summary>
    public BleAddress Address { get; }

    /// <summary> Completes once, when the connection ended, with why it ended </summary>
    public Task<DisconnectInfo> Disconnected => _disconnected.Task;

    /// <summary> Ends the connection. Calling it again, or after the peripheral disconnected, waits for the same end </summary>
    /// <param name="cancellationToken"> Stops waiting; the connection still ends </param>
    /// <returns> A task that completes once the connection ended </returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        End(new DisconnectInfo(DisconnectReason.LocalRequest));
        await _disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Ends the connection; the same as <see cref="DisconnectAsync"/> </summary>
    /// <returns> A task that completes once the connection ended </returns>
    public ValueTask DisposeAsync() => new(DisconnectAsync(CancellationToken.None));

    /// <summary> Ends the connection exactly once; the first reason wins </summary>
    internal void End(DisconnectInfo info)
    {
        lock (_lock)
        {
            if (_ending is not null)
                return;
            _ending = info;
        }
        // Off the caller's thread: a driver may report a disconnect from its read loop, and teardown calls back into it
        _ = Task.Run(() => TeardownAsync(info));
    }

    private async Task TeardownAsync(DisconnectInfo info)
    {
        try
        {
            await _driver.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(Address, e);
        }
        finally
        {
            _central.Remove(this);
            _logger.LogDisconnected(Address, info.Reason);
            _disconnected.TrySetResult(info);
        }
    }
}
