using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> The central role of an adapter: connects to peripherals </summary>
public sealed class BleCentral
{
    private readonly ICentralDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly List<ConnectedPeripheral> _connections = [];

    // Disposal waits for every connection attempt it admitted
    private readonly OperationTracker _attempts = new();

    internal BleCentral(BleAdapter adapter, ICentralDriver driver, ILogger logger)
    {
        Adapter = adapter;
        _driver = driver;
        _logger = logger;
    }

    /// <summary> The adapter this central belongs to </summary>
    public BleAdapter Adapter { get; }

    /// <summary> The open connections, as a snapshot </summary>
    public IReadOnlyList<ConnectedPeripheral> Connections
    {
        get
        {
            lock (_lock)
                return [.. _connections];
        }
    }

    /// <summary> Connects to a peripheral </summary>
    /// <param name="address"> The address of the peripheral </param>
    /// <param name="parameters"> How the connection runs; the defaults when null </param>
    /// <param name="cancellationToken"> Cancels connecting </param>
    /// <returns> The connection once the link is up. The caller owns it; disposing the adapter ends it too </returns>
    /// <exception cref="BleConnectException"> The peripheral could not be connected; <see cref="BleConnectException.Kind"/> says why </exception>
    public async Task<ConnectedPeripheral> ConnectAsync(
        BleAddress address,
        BleConnectionParameters? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(address);
        parameters ??= new BleConnectionParameters();
        parameters.Validate(nameof(parameters));
        _attempts.Enter(typeof(BleCentral));
        try
        {
            _logger.LogConnecting(address);
            var events = new ConnectionEvents();
            IConnectionDriver driver;
            try
            {
                driver = await _driver
                    .ConnectAsync(address, parameters, events, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BleConnectException e)
            {
                _logger.LogConnectFailed(address, e.Kind, e);
                throw;
            }
            var connection = new ConnectedPeripheral(this, address, driver, _logger);
            bool admitted;
            lock (_lock)
            {
                // A link that comes up after the caller gave up, or while the adapter closes, is never handed out
                admitted = !_attempts.IsClosed && !cancellationToken.IsCancellationRequested;
                if (admitted)
                    _connections.Add(connection);
            }
            events.Attach(connection);
            if (!admitted)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                throw new ObjectDisposedException(nameof(BleCentral));
            }
            _logger.LogConnected(address);
            return connection;
        }
        finally
        {
            _attempts.Exit();
        }
    }

    internal void Remove(ConnectedPeripheral connection)
    {
        lock (_lock)
            _connections.Remove(connection);
    }

    internal void CloseAdmission() => _attempts.Close();

    /// <summary> Waits for connection attempts in flight, then ends every connection </summary>
    internal async Task DisposeAsync()
    {
        await _attempts.CloseAndWaitAsync().ConfigureAwait(false);
        foreach (ConnectedPeripheral connection in Connections)
            await connection.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
