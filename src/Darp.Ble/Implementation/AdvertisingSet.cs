using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Implementation;

/// <summary> The default implementation for an advertising set </summary>
/// <param name="broadcaster"> The broadcaster of this advertisement </param>
public abstract class AdvertisingSet(BleBroadcaster broadcaster) : IAdvertisingSet
{
    private readonly BleBroadcaster _broadcaster = broadcaster;
    private bool _isDisposing;

    /// <inheritdoc />
    public IBleBroadcaster Broadcaster => _broadcaster;

    /// <inheritdoc />
    public BleAddress RandomAddress { get; protected set; } = BleAddress.NotAvailable;

    /// <inheritdoc />
    public AdvertisingParameters Parameters { get; protected set; } = AdvertisingParameters.Default;

    /// <inheritdoc />
    public AdvertisingData Data { get; protected set; } = AdvertisingData.Empty;

    /// <inheritdoc />
    public AdvertisingData? ScanResponseData { get; protected set; }

    /// <inheritdoc />
    public TxPowerLevel SelectedTxPower { get; protected set; } = TxPowerLevel.NotAvailable;

    /// <inheritdoc />
    public bool IsAdvertising { get; protected set; }

    /// <inheritdoc />
    public virtual Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RandomAddress = randomAddress;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetAdvertisingParametersAsync(
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        Parameters = parameters;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetAdvertisingDataAsync(AdvertisingData data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Data = data;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetScanResponseDataAsync(
        AdvertisingData scanResponseData,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ScanResponseData = scanResponseData;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposing)
            return;
        _isDisposing = true;
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary> Remove the advertising set </summary>
    /// <returns> The value task </returns>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <summary> Throws if the disposal of this set was started </summary>
    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposing, this);
}
