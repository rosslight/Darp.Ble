using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> An advertising set: what an adapter advertises, and how </summary>
/// <remarks>
/// Its operations run one at a time. Its state changes only after the backend accepted a change.
/// A central that connects through the set stops it; with autoRestart the set starts again once that central disconnected.
/// A set the backend stopped because of an error, e.g. the radio turned off, stays stopped until it is started again
/// </remarks>
[SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "The semaphore outlives disposal: late calls still wait on it to see the disposed state. It holds no wait handle"
)]
public sealed class AdvertisingSet : IAsyncDisposable
{
    private readonly BleBroadcaster _broadcaster;
    private readonly IAdvertisingSetDriver _driver;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly Lazy<Task> _disposal;
    private readonly bool _autoRestart;

    // Guards the fields below: the driver's thread stops the set when a central connects, without waiting for the operations
    private readonly Lock _lock = new();

    // Set before the driver starts, as a central may connect as soon as the set advertises
    private bool _isAdvertising;

    // The restart after the connected central disconnects; a command or disposal cancels it
    private CancellationTokenSource? _pendingRestart;

    // A stop the app asked for runs; a central that connects meanwhile must not restart the set
    private bool _isStopping;

    // Set by disposal under the operations, so later calls fail
    private bool _isDisposed;

    // Restarts after a disconnect; disposal waits for them
    private readonly OperationTracker _restarts = new();

    internal AdvertisingSet(
        BleBroadcaster broadcaster,
        IAdvertisingSetDriver driver,
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        bool autoRestart,
        ILogger logger
    )
    {
        _autoRestart = autoRestart;
        _broadcaster = broadcaster;
        _driver = driver;
        _logger = logger;
        Parameters = parameters;
        Data = data;
        ScanResponseData = scanResponseData;
        _disposal = new Lazy<Task>(DisposeCoreAsync);
    }

    /// <summary> The broadcaster this set belongs to </summary>
    public BleBroadcaster Broadcaster => _broadcaster;

    /// <summary> The address the set advertises with </summary>
    public BleAddress RandomAddress => _driver.RandomAddress;

    /// <summary> How the set advertises </summary>
    public AdvertisingParameters Parameters { get; private set; }

    /// <summary> The advertising data </summary>
    public AdvertisingData Data { get; private set; }

    /// <summary> The scan response data, if the set has one </summary>
    public AdvertisingData? ScanResponseData { get; private set; }

    /// <summary> Starts advertising; does nothing if the set already advertises </summary>
    /// <param name="cancellationToken"> Cancels starting </param>
    /// <returns> A task that completes once the set advertises </returns>
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        RunAsync(
            () =>
            {
                CancelPendingRestart();
                return StartCoreAsync(cancellationToken);
            },
            cancellationToken
        );

    /// <summary> Stops advertising; does nothing if the set does not advertise </summary>
    /// <param name="cancellationToken"> Cancels stopping </param>
    /// <returns> A task that completes once the set stopped </returns>
    public Task StopAsync(CancellationToken cancellationToken = default) =>
        RunAsync(() => StopCoreAsync(cancellationToken), cancellationToken);

    /// <summary> Changes how the set advertises. Controllers reject this while advertising </summary>
    /// <param name="parameters"> The new parameters </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the parameters apply </returns>
    /// <exception cref="InvalidOperationException"> The set advertises; use <see cref="SetAdvertisingParametersAndRestartAsync"/> </exception>
    public Task SetAdvertisingParametersAsync(
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate(nameof(parameters));
        return RunAsync(
            async () =>
            {
                if (IsAdvertising)
                {
                    throw new InvalidOperationException(
                        "The parameters of an advertising set cannot change while it advertises"
                    );
                }
                await _driver.SetParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
                Parameters = parameters;
            },
            cancellationToken
        );
    }

    /// <summary> Changes how the set advertises; a set that advertises is stopped first and started again afterwards </summary>
    /// <param name="parameters"> The new parameters </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the parameters apply. If starting again fails, the set stays stopped </returns>
    public Task SetAdvertisingParametersAndRestartAsync(
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate(nameof(parameters));
        return RunAsync(
            async () =>
            {
                bool wasAdvertising = IsAdvertising;
                await StopCoreAsync(cancellationToken).ConfigureAwait(false);
                await _driver.SetParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
                Parameters = parameters;
                if (wasAdvertising)
                    await StartCoreAsync(cancellationToken).ConfigureAwait(false);
            },
            cancellationToken
        );
    }

    /// <summary> Changes the advertising data, also while advertising </summary>
    /// <param name="data"> The new data </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the data apply </returns>
    public Task SetAdvertisingDataAsync(AdvertisingData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RunAsync(
            async () =>
            {
                await _driver.SetDataAsync(data, cancellationToken).ConfigureAwait(false);
                Data = data;
            },
            cancellationToken
        );
    }

    /// <summary> Changes the scan response data, also while advertising </summary>
    /// <param name="data"> The new data </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the data apply </returns>
    public Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RunAsync(
            async () =>
            {
                await _driver.SetScanResponseDataAsync(data, cancellationToken).ConfigureAwait(false);
                ScanResponseData = data;
            },
            cancellationToken
        );
    }

    /// <summary> Changes the address the set advertises with </summary>
    /// <param name="address"> The new address </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the address applies </returns>
    /// <exception cref="NotSupportedException"> The adapter cannot set random addresses; see <see cref="BleAdapter.Capabilities"/> </exception>
    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        _broadcaster.Adapter.EnsureCanSetRandomAddress();
        return RunAsync(() => _driver.SetRandomAddressAsync(address, cancellationToken), cancellationToken);
    }

    /// <summary> Stops advertising and removes the set. Calling it again waits for the same disposal </summary>
    /// <returns> A task that completes once the set is removed </returns>
    public ValueTask DisposeAsync() => new(_disposal.Value);

    private async Task DisposeCoreAsync()
    {
        await _operations.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            lock (_lock)
                _isDisposed = true;
            // Closed first: a central connecting now admits no restart that disposal would wait for
            CloseAdmission();
            await StopCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
            try
            {
                await _restarts.CloseAndWaitAsync().ConfigureAwait(false);
                await _driver.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                // Only now, so adapter disposal waits for this set's driver
                _broadcaster.Remove(this);
            }
        }
    }

    /// <summary> The adapter closes: no restart starts any more </summary>
    internal void CloseAdmission()
    {
        _restarts.Close();
        CancelPendingRestart();
    }

    /// <summary> A central connected through the set, which stopped. Called on the driver's thread </summary>
    internal void OnStoppedByConnection(ICentralConnectionDriver central)
    {
        CancellationTokenSource? restart = null;
        lock (_lock)
        {
            if (!_isAdvertising)
                return;
            _isAdvertising = false;
            // A stop the app asked for, or a closing adapter, wins over the restart
            if (_autoRestart && !_isStopping && _restarts.TryEnter())
            {
                restart = new CancellationTokenSource();
                _pendingRestart = restart;
            }
        }
        _logger.LogAdvertisingStopped(RandomAddress);
        if (restart is not null)
            _ = RestartAfterDisconnectAsync(central, restart);
    }

    /// <summary> The backend stopped the set because of an error. Called on the driver's thread </summary>
    internal void OnFailed(BleException error)
    {
        lock (_lock)
        {
            if (!_isAdvertising)
                return;
            _isAdvertising = false;
        }
        _logger.LogAdvertisingFailed(RandomAddress, error);
    }

    /// <summary> Starts the set again once the central disconnected, unless a command or disposal cancelled the restart </summary>
    private async Task RestartAfterDisconnectAsync(ICentralConnectionDriver central, CancellationTokenSource restart)
    {
        try
        {
            // Not found: the central already disconnected
            ConnectedCentral? connected = _broadcaster.Adapter.FindCentral(central);
            if (connected is not null)
                await connected.Disconnected.WaitAsync(restart.Token).ConfigureAwait(false);
            await RunAsync(
                    async () =>
                    {
                        lock (_lock)
                        {
                            // A command since the stop decides the set's state instead
                            if (!ReferenceEquals(_pendingRestart, restart))
                                return;
                            _pendingRestart = null;
                        }
                        await StartCoreAsync(CancellationToken.None).ConfigureAwait(false);
                    },
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (restart.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (IsDisposed) { }
        catch (Exception e)
        {
            _logger.LogAdvertisingRestartFailed(RandomAddress, e);
        }
        finally
        {
            lock (_lock)
            {
                if (ReferenceEquals(_pendingRestart, restart))
                    _pendingRestart = null;
            }
            restart.Dispose();
            _restarts.Exit();
        }
    }

    private bool IsAdvertising
    {
        get
        {
            lock (_lock)
                return _isAdvertising;
        }
    }

    private bool IsDisposed
    {
        get
        {
            lock (_lock)
                return _isDisposed;
        }
    }

    private void CancelPendingRestart()
    {
        lock (_lock)
        {
            _pendingRestart?.Cancel();
            _pendingRestart = null;
        }
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_isAdvertising)
                return;
            // Before the driver call: a central may connect as soon as the set advertises
            _isAdvertising = true;
        }
        try
        {
            await _driver.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
                _isAdvertising = false;
            throw;
        }
        _logger.LogAdvertisingStarted(RandomAddress);
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        // In one step with the connection callback's check: a central connecting now admits no restart
        lock (_lock)
        {
            _pendingRestart?.Cancel();
            _pendingRestart = null;
            if (!_isAdvertising)
                return;
            _isStopping = true;
        }
        try
        {
            await _driver.StopAsync(cancellationToken).ConfigureAwait(false);
            lock (_lock)
                _isAdvertising = false;
        }
        finally
        {
            lock (_lock)
                _isStopping = false;
        }
        _logger.LogAdvertisingStopped(RandomAddress);
    }

    private async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
        }
    }
}
