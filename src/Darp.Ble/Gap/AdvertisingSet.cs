using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> An advertising set: what an adapter advertises, and how </summary>
/// <remarks> Its operations run one at a time. Its state changes only after the backend accepted a change </remarks>
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
    private bool _isAdvertising;
    private bool _isDisposed;

    internal AdvertisingSet(
        BleBroadcaster broadcaster,
        IAdvertisingSetDriver driver,
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        ILogger logger
    )
    {
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
            async () =>
            {
                if (_isAdvertising)
                    return;
                await _driver.StartAsync(cancellationToken).ConfigureAwait(false);
                _isAdvertising = true;
                _logger.LogAdvertisingStarted(RandomAddress);
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
                if (_isAdvertising)
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
                bool wasAdvertising = _isAdvertising;
                await StopCoreAsync(cancellationToken).ConfigureAwait(false);
                await _driver.SetParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
                Parameters = parameters;
                if (!wasAdvertising)
                    return;
                await _driver.StartAsync(cancellationToken).ConfigureAwait(false);
                _isAdvertising = true;
                _logger.LogAdvertisingStarted(RandomAddress);
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
            _isDisposed = true;
            await StopCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
            _broadcaster.Remove(this);
            await _driver.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        if (!_isAdvertising)
            return;
        await _driver.StopAsync(cancellationToken).ConfigureAwait(false);
        _isAdvertising = false;
        _logger.LogAdvertisingStopped(RandomAddress);
    }

    private async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
        }
    }
}
