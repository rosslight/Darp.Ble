using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Backends;

/// <summary> The services a <see cref="BleManager"/> gives to its backends and their drivers </summary>
[Experimental(Experiments.Backends)]
public sealed class BleBackendContext
{
    internal BleBackendContext(ILoggerFactory loggerFactory, TimeProvider timeProvider, DiagnosticsSettings diagnostics)
    {
        LoggerFactory = loggerFactory;
        TimeProvider = timeProvider;
        IncludeAddresses = diagnostics.IncludeAddresses;
    }

    /// <summary> Creates the loggers of backends and drivers </summary>
    public ILoggerFactory LoggerFactory { get; }

    /// <summary> The only clock: every timer and timestamp of a backend uses it </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary> Whether the backend's logs and spans may carry peer addresses; see <see cref="BleDiagnosticsOptions.IncludeAddresses"/> </summary>
    public bool IncludeAddresses { get; }
}
