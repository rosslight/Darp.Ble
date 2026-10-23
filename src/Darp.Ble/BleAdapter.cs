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

    internal BleAdapter(string backend, string? name, IBleAdapterDriver driver, BleCoreOptions options, ILogger logger)
    {
        Backend = backend;
        Name = name;
        Capabilities = driver.Capabilities;
        _driver = driver;
        _logger = logger;
        _disposal = new Lazy<Task>(DisposeCoreAsync);
        EnsureRoleMatches(Capabilities.Observer, driver.Observer);
        if (driver.Observer is not null)
            _observer = new BleObserver(this, driver.Observer, options.AdvertisementQueue, logger);
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

    /// <summary> Closes the adapter: ends its scans, then releases its driver. Calling it again waits for the same disposal </summary>
    /// <returns> A task that completes once the driver is disposed </returns>
    public ValueTask DisposeAsync() => new(_disposal.Value);

    private async Task DisposeCoreAsync()
    {
        try
        {
            if (_observer is not null)
                await _observer.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await _driver.DisposeAsync().ConfigureAwait(false);
            _logger.LogAdapterDisposed(Backend, Name);
        }
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
