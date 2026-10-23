using System.Runtime.ExceptionServices;
using Darp.Ble.Backends;
using Darp.Ble.Connections;
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
    private readonly BleCentral? _central;
    private readonly BlePeripheral? _peripheral;

    // Adapter-level operations, e.g. changing the address; disposal waits for them before releasing the driver
    private readonly OperationTracker _operations = new();

    internal BleAdapter(
        string backend,
        string? name,
        IBleAdapterDriver driver,
        BleCoreOptions options,
        TimeProvider timeProvider,
        ILogger logger
    )
    {
        Backend = backend;
        Name = name;
        Capabilities = driver.Capabilities;
        _driver = driver;
        _logger = logger;
        _disposal = new Lazy<Task>(DisposeCoreAsync);
        EnsureRoleMatches(Capabilities.Observer, driver.Observer);
        EnsureRoleMatches(Capabilities.Broadcaster, driver.Broadcaster);
        EnsureRoleMatches(Capabilities.Central, driver.Central);
        EnsureRoleMatches(Capabilities.Peripheral, driver.Peripheral);
        if (driver.Observer is not null)
            _observer = new BleObserver(this, driver.Observer, options.AdvertisementQueue, logger);
        if (driver.Broadcaster is not null)
            _broadcaster = new BleBroadcaster(this, driver.Broadcaster, logger);
        if (driver.Central is not null)
            _central = new BleCentral(this, driver.Central, timeProvider, logger);
        if (driver.Peripheral is not null)
            _peripheral = new BlePeripheral(this, driver.Peripheral, logger);
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

    /// <summary> Connects to peripherals </summary>
    /// <exception cref="NotSupportedException"> The adapter cannot connect to peripherals; see <see cref="Capabilities"/> </exception>
    public BleCentral Central => _central ?? throw MissingRole(nameof(Central));

    /// <summary> Accepts connections from centrals </summary>
    /// <exception cref="NotSupportedException"> Centrals cannot connect to the adapter; see <see cref="Capabilities"/> </exception>
    public BlePeripheral Peripheral => _peripheral ?? throw MissingRole(nameof(Peripheral));

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
        _central?.CloseAdmission();
        _peripheral?.CloseAdmission();
        // Every step runs even if an earlier one fails
        List<Exception> failures = [];
        if (_observer is not null)
            await RunStepAsync(_observer.DisposeAsync, failures).ConfigureAwait(false);
        if (_broadcaster is not null)
            await RunStepAsync(_broadcaster.DisposeAsync, failures).ConfigureAwait(false);
        if (_central is not null)
            await RunStepAsync(_central.DisposeAsync, failures).ConfigureAwait(false);
        if (_peripheral is not null)
            await RunStepAsync(_peripheral.DisposeAsync, failures).ConfigureAwait(false);
        await _operations.CloseAndWaitAsync().ConfigureAwait(false);
        await RunStepAsync(() => _driver.DisposeAsync().AsTask(), failures).ConfigureAwait(false);
        _logger.LogAdapterDisposed(Backend, Name);
        if (failures.Count == 1)
            ExceptionDispatchInfo.Throw(failures[0]);
        if (failures.Count > 1)
            throw new AggregateException("Disposing the adapter failed in several steps", failures);
    }

    private static async Task RunStepAsync(Func<Task> step, List<Exception> failures)
    {
        try
        {
            await step().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            failures.Add(e);
        }
    }

    internal void EnsureCanSetRandomAddress()
    {
        if (!Capabilities.HasFlag(Capabilities.SetRandomAddress))
            throw new NotSupportedException($"The adapter {Name} of backend {Backend} cannot set random addresses");
    }

    private void EnsureRoleMatches(Capabilities role, object? roleDriver)
    {
        if (Capabilities.HasFlag(role) != roleDriver is not null)
        {
            throw new InvalidOperationException(
                $"The backend {Backend} reports {Capabilities}, which does not match its {role} driver"
            );
        }
    }

    private NotSupportedException MissingRole(string role) =>
        new($"The adapter {Name} of backend {Backend} has no {role} role; see its Capabilities");
}
