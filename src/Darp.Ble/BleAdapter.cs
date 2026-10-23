using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> An opened adapter: the local radio, e.g. a USB dongle or the computer's Bluetooth </summary>
/// <remarks> Opened by <see cref="BleAdapterInfo.OpenAsync"/>. The caller owns it; disposing it ends everything started on it </remarks>
public sealed class BleAdapter : IAsyncDisposable
{
    private readonly IBleAdapterDriver _driver;
    private readonly ILogger _logger;
    private readonly Lazy<Task> _disposal;
    private readonly BleObserver? _observer;
    private readonly BleBroadcaster? _broadcaster;

    // Adapter-level operations, e.g. changing the address; disposal waits for them before releasing the driver
    private readonly OperationTracker _operations = new();

    internal BleAdapter(string backend, string? name, IBleAdapterDriver driver, BleCoreOptions options, ILogger logger)
    {
        Backend = backend;
        Name = name;
        _driver = driver;
        _logger = logger;
        _disposal = new Lazy<Task>(DisposeCoreAsync);
        Capabilities =
            driver.Features
            | (driver.Observer is null ? Capabilities.None : Capabilities.Observer)
            | (driver.Broadcaster is null ? Capabilities.None : Capabilities.Broadcaster);
        if (driver.Observer is not null)
            _observer = new BleObserver(this, driver.Observer, options.AdvertisementQueue, logger);
        if (driver.Broadcaster is not null)
            _broadcaster = new BleBroadcaster(this, driver.Broadcaster, logger);
    }

    /// <summary> The backend of the adapter, e.g. <see cref="BleBackends.Mock"/> </summary>
    public string Backend { get; }

    /// <summary> The name of the adapter, if the backend knows one </summary>
    public string? Name { get; }

    /// <summary> The roles and features this adapter supports </summary>
    public Capabilities Capabilities { get; }

    /// <summary> Scans for advertisements </summary>
    /// <exception cref="NotSupportedException"> The adapter cannot scan; see <see cref="Capabilities"/> </exception>
    public BleObserver Observer => _observer ?? throw MissingRole(nameof(Observer));

    /// <summary> Advertises with advertising sets </summary>
    /// <exception cref="NotSupportedException"> The adapter cannot advertise; see <see cref="Capabilities"/> </exception>
    public BleBroadcaster Broadcaster => _broadcaster ?? throw MissingRole(nameof(Broadcaster));

    /// <summary> The adapter's own random address </summary>
    public BleAddress RandomAddress => _driver.RandomAddress;

    /// <summary> Changes the adapter's own random address </summary>
    /// <param name="address"> The new address </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the address applies </returns>
    /// <exception cref="NotSupportedException"> The adapter cannot set random addresses; see <see cref="Capabilities"/> </exception>
    public async Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        EnsureCanSetRandomAddress();
        _operations.Enter(typeof(BleAdapter));
        try
        {
            await _driver.SetRandomAddressAsync(address, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operations.Exit();
        }
    }

    /// <summary> Closes the adapter: ends its scans and advertising, then releases its driver. Calling it again waits for the same disposal </summary>
    /// <returns> A task that completes once the driver is disposed </returns>
    public ValueTask DisposeAsync() => new(_disposal.Value);

    private async Task DisposeCoreAsync()
    {
        // Nothing new may start while the adapter shuts down
        _operations.Close();
        _observer?.CloseAdmission();
        _broadcaster?.CloseAdmission();
        // Every step runs even if an earlier one fails
        try
        {
            if (_observer is not null)
                await _observer.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (_broadcaster is not null)
                    await _broadcaster.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await _operations.CloseAndWaitAsync().ConfigureAwait(false);
                await _driver.DisposeAsync().ConfigureAwait(false);
                _logger.LogAdapterDisposed(Backend, Name);
            }
        }
    }

    internal void EnsureCanSetRandomAddress()
    {
        if (!Capabilities.HasFlag(Capabilities.SetRandomAddress))
            throw new NotSupportedException($"The adapter {Name} of backend {Backend} cannot set random addresses");
    }

    private NotSupportedException MissingRole(string role) =>
        new($"The adapter {Name} of backend {Backend} has no {role} role; see its Capabilities");
}
