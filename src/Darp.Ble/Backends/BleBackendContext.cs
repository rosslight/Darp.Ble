using Microsoft.Extensions.Logging;

namespace Darp.Ble.Backends;

/// <summary> The services a <see cref="BleManager"/> gives to its backends and their drivers </summary>
/// <param name="loggerFactory"> Creates the loggers of backends and drivers </param>
/// <param name="timeProvider"> The only clock: every timer and timestamp of a backend uses it </param>
public sealed class BleBackendContext(ILoggerFactory loggerFactory, TimeProvider timeProvider)
{
    /// <summary> Creates the loggers of backends and drivers </summary>
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;

    /// <summary> The only clock: every timer and timestamp of a backend uses it </summary>
    public TimeProvider TimeProvider { get; } = timeProvider;
}
