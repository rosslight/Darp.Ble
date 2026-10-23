using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> The central role of an adapter: connects to peripherals </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The role ends with its adapter, whose disposal disposes it"
)]
public sealed class BleCentral
{
    private readonly ICentralDriver _driver;
    private readonly BleObserver _observer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly List<ConnectedPeripheral> _connections = [];

    // Disposal cancels the connection attempts it admitted and waits for them
    private readonly OperationTracker _attempts = new();
    private readonly CancellationTokenSource _closing = new();

    internal BleCentral(
        BleAdapter adapter,
        ICentralDriver driver,
        BleObserver observer,
        TimeProvider timeProvider,
        ILogger logger
    )
    {
        Adapter = adapter;
        _driver = driver;
        _observer = observer;
        _timeProvider = timeProvider;
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
    /// <remarks> The adapter's scans pause while the link comes up and continue afterwards </remarks>
    /// <param name="address"> The address of the peripheral </param>
    /// <param name="parameters"> How the connection runs; the defaults when null </param>
    /// <param name="cancellationToken"> Cancels connecting </param>
    /// <returns> The connection once the link is up. The caller owns it; disposing the adapter ends it too </returns>
    /// <exception cref="BleConnectException"> The peripheral could not be connected; <see cref="BleConnectException.Kind"/> says why </exception>
    /// <exception cref="BleDisconnectedException"> The link went down while the connection was being set up </exception>
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
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closing.Token);
            try
            {
                return await ConnectCoreAsync(address, parameters, attempt.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ObjectDisposedException("The adapter was disposed while connecting", e);
            }
        }
        finally
        {
            _attempts.Exit();
        }
    }

    private async Task<ConnectedPeripheral> ConnectCoreAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogConnecting(address);
        var events = new ConnectionEvents();
        IConnectionDriver driver;
        // The adapter's scans pause until the link is up (D6)
        await _observer.PauseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            driver = await _driver.ConnectAsync(address, parameters, events, cancellationToken).ConfigureAwait(false);
        }
        catch (BleConnectException e)
        {
            _logger.LogConnectFailed(address, e.Kind, e);
            throw;
        }
        finally
        {
            await _observer.ResumeAsync().ConfigureAwait(false);
        }
        // Until the caller gets it, this method owns the connection
        var connection = new ConnectedPeripheral(this, address, driver, _timeProvider, _logger);
        events.Attach(connection);
        try
        {
            await connection.ExchangeMtuAsync(cancellationToken).ConfigureAwait(false);
            lock (_lock)
            {
                // A link that comes up after the caller gave up, or while the adapter closes, is never handed out
                cancellationToken.ThrowIfCancellationRequested();
                // Checked under this lock: a teardown that starts later removes the connection after it was added
                if (connection.Ending is { } ending)
                    throw new BleDisconnectedException(ending);
                _connections.Add(connection);
            }
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        _logger.LogConnected(address);
        return connection;
    }

    internal void Remove(ConnectedPeripheral connection)
    {
        lock (_lock)
            _connections.Remove(connection);
    }

    internal void CloseAdmission()
    {
        _attempts.Close();
        _closing.Cancel();
    }

    /// <summary> Cancels connection attempts in flight and waits for them, then ends every connection </summary>
    internal async Task DisposeAsync()
    {
        CloseAdmission();
        await _attempts.CloseAndWaitAsync().ConfigureAwait(false);
        _closing.Dispose();
        foreach (ConnectedPeripheral connection in Connections)
            await connection.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
