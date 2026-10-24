using System.Diagnostics;
using Darp.Ble.Backends;
using Darp.Ble.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> An adapter that can be opened, as listed by <see cref="BleManager.EnumerateAdaptersAsync"/> </summary>
public sealed class BleAdapterInfo
{
    private readonly IBleAdapterSource _source;
    private readonly BleBackendContext _context;
    private readonly BleCoreOptions _options;
    private readonly ILogger _logger;

    internal BleAdapterInfo(
        string backend,
        IBleAdapterSource source,
        BleBackendContext context,
        BleCoreOptions options,
        ILogger logger
    )
    {
        Backend = backend;
        Name = source.Name;
        _source = source;
        _context = context;
        _options = options;
        _logger = logger;
    }

    /// <summary> The backend of the adapter, e.g. <see cref="BleBackends.Mock"/> </summary>
    public string Backend { get; }

    /// <summary> The name of the adapter, if the backend knows one </summary>
    public string? Name { get; }

    /// <summary> Opens the adapter </summary>
    /// <param name="cancellationToken"> Cancels opening </param>
    /// <returns> The ready adapter. The caller owns it; disposing it ends everything started on it </returns>
    /// <exception cref="BleOpenException"> The adapter could not be opened; <see cref="BleOpenException.Kind"/> says why </exception>
    public async Task<BleAdapter> OpenAsync(CancellationToken cancellationToken = default)
    {
        // From the driver's open to the ready adapter (D14)
        using Activity? activity = Telemetry.Source.StartActivity("Open adapter {Backend}");
        if (activity is { IsAllDataRequested: true })
            activity.SetTag(Telemetry.BackendTag, Backend);
        try
        {
            BleAdapter adapter = await OpenCoreAsync(cancellationToken).ConfigureAwait(false);
            Telemetry.Finish(activity, Telemetry.Success);
            return adapter;
        }
        catch (Exception e)
        {
            Telemetry.Finish(activity, Telemetry.OutcomeOf(e), e);
            throw;
        }
    }

    private async Task<BleAdapter> OpenCoreAsync(CancellationToken cancellationToken)
    {
        IBleAdapterDriver driver;
        try
        {
            driver = await _source.OpenAsync(_context, cancellationToken).ConfigureAwait(false);
        }
        catch (BleOpenException e)
        {
            _logger.LogAdapterOpenFailed(Backend, Name, e.Kind, e);
            throw;
        }

        // Until the adapter exists, this method owns the driver
        try
        {
            var adapter = new BleAdapter(Backend, Name, driver, _options, _context.TimeProvider, _logger);
            _logger.LogAdapterOpened(Backend, Name);
            return adapter;
        }
        catch
        {
            await driver.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
