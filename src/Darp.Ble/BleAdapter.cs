using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> An opened adapter: the local radio, e.g. a USB dongle or the computer's Bluetooth </summary>
/// <remarks> Opened by <see cref="BleAdapterInfo.OpenAsync"/>. The caller owns it; disposing it ends everything started on it </remarks>
public sealed class BleAdapter : IAsyncDisposable
{
    private readonly IBleAdapterDriver _driver;
    private readonly ILogger _logger;
    private readonly Lazy<Task> _disposal;

    internal BleAdapter(string backend, string? name, IBleAdapterDriver driver, ILogger logger)
    {
        Backend = backend;
        Name = name;
        _driver = driver;
        Capabilities = driver.Features;
        _logger = logger;
        _disposal = new Lazy<Task>(DisposeCoreAsync);
    }

    /// <summary> The backend of the adapter, e.g. <see cref="BleBackends.Mock"/> </summary>
    public string Backend { get; }

    /// <summary> The name of the adapter, if the backend knows one </summary>
    public string? Name { get; }

    /// <summary> The roles this adapter supports </summary>
    public Capabilities Capabilities { get; }

    /// <summary> Closes the adapter and releases its driver. Calling it again waits for the same disposal </summary>
    /// <returns> A task that completes once the driver is disposed </returns>
    public ValueTask DisposeAsync() => new(_disposal.Value);

    private async Task DisposeCoreAsync()
    {
        await _driver.DisposeAsync().ConfigureAwait(false);
        _logger.LogAdapterDisposed(Backend, Name);
    }
}
