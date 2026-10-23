using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> A central connected to the adapter's peripheral role. It ends once; afterwards it cannot be used again </summary>
public sealed class ConnectedCentral
{
    private readonly BlePeripheral _peripheral;
    private readonly IPeripheralDriver _driver;
    private readonly ICentralConnectionDriver _connection;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private DisconnectInfo? _ending;

    internal ConnectedCentral(
        BlePeripheral peripheral,
        IPeripheralDriver driver,
        ICentralConnectionDriver connection,
        ILogger logger
    )
    {
        _peripheral = peripheral;
        _driver = driver;
        _connection = connection;
        _logger = logger;
        Address = connection.Address;
    }

    /// <summary> The address of the central </summary>
    public BleAddress Address { get; }

    /// <summary> Completes once, when the connection ended, with why it ended </summary>
    public Task<DisconnectInfo> Disconnected => _disconnected.Task;

    /// <summary> Ends the connection. Calling it again, or after the central disconnected, waits for the same end </summary>
    /// <param name="cancellationToken"> Stops waiting; the connection still ends </param>
    /// <returns> A task that completes once the connection ended </returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        End(new DisconnectInfo(DisconnectReason.LocalRequest), takeLinkDown: true);
        await _disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> The peripheral role this central is connected to </summary>
    internal BlePeripheral Peripheral => _peripheral;

    /// <summary> Sends a notification to this central </summary>
    internal ValueTask NotifyAsync(
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        DisconnectInfo? ending;
        lock (_lock)
            ending = _ending;
        if (ending is not null)
            throw new BleDisconnectedException(ending);
        return _driver.NotifyAsync(_connection, characteristic, value, cancellationToken);
    }

    /// <summary> Runs a hosted handler for this central. An unexpected failure answers with <see cref="GattProtocolStatus.UnlikelyError"/> </summary>
    internal async Task<T> RunHandlerAsync<T>(Func<CancellationToken, ValueTask<T>> handler)
    {
        try
        {
            return await handler(CancellationToken.None).ConfigureAwait(false);
        }
        catch (GattException e) when (e.AttStatus is not null)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogHandlerFailed(Address, e);
            throw new GattException(GattProtocolStatus.UnlikelyError, "The handler failed", e);
        }
    }

    /// <summary> Ends the connection exactly once; the first reason wins </summary>
    /// <param name="info"> Why it ended </param>
    /// <param name="takeLinkDown"> Whether the driver still has to take the link down; not when it reported the end </param>
    internal void End(DisconnectInfo info, bool takeLinkDown)
    {
        lock (_lock)
        {
            if (_ending is not null)
                return;
            _ending = info;
        }
        // Off the caller's thread: the driver reports a disconnect from its own loop
        _ = Task.Run(() => TeardownAsync(info, takeLinkDown));
    }

    private async Task TeardownAsync(DisconnectInfo info, bool takeLinkDown)
    {
        try
        {
            if (takeLinkDown)
                await _driver.DisconnectAsync(_connection, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(Address, e);
        }
        finally
        {
            _peripheral.Remove(_connection);
            _logger.LogCentralDisconnected(Address, info.Reason);
            _disconnected.TrySetResult(info);
        }
    }
}
