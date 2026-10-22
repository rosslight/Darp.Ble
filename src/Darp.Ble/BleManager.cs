using Darp.Ble.Backends;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble;

/// <summary> The entry point: lists the adapters of all registered backends </summary>
public sealed class BleManager
{
    private readonly IBleBackend[] _backends;
    private readonly BleBackendContext _context;
    private readonly ILogger _logger;

    /// <summary> Creates a manager with the backends the configuration adds </summary>
    /// <param name="configure"> Adds backends, e.g. <c>builder => builder.AddMock()</c> </param>
    /// <param name="loggerFactory"> Creates the library's loggers; no logging when null </param>
    /// <param name="timeProvider"> The only clock of the library and its backends; the system clock when null </param>
    public BleManager(
        Action<BleManagerBuilder> configure,
        ILoggerFactory? loggerFactory = null,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new BleManagerBuilder();
        configure(builder);
        _backends = builder.Build();
        _context = new BleBackendContext(
            loggerFactory ?? NullLoggerFactory.Instance,
            timeProvider ?? TimeProvider.System
        );
        _logger = _context.LoggerFactory.CreateLogger(Logging.Category);
    }

    /// <summary> Lists the adapters of all backends, in the order the backends were added </summary>
    /// <param name="cancellationToken"> Cancels the enumeration </param>
    /// <returns> The adapters that can be opened right now </returns>
    public async Task<IReadOnlyList<BleAdapterInfo>> EnumerateAdaptersAsync(
        CancellationToken cancellationToken = default
    )
    {
        List<BleAdapterInfo> adapters = [];
        foreach (IBleBackend backend in _backends)
        {
            IReadOnlyList<IBleAdapterSource> sources = await backend
                .EnumerateAdaptersAsync(_context, cancellationToken)
                .ConfigureAwait(false);
            foreach (IBleAdapterSource source in sources)
                adapters.Add(new BleAdapterInfo(backend.Name, source, _context, _logger));
        }
        return adapters;
    }
}
