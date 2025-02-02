using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> The base manager class. Holds all implementations </summary>
public sealed class BleManager(IReadOnlyCollection<IBleFactory> factories, ILoggerFactory loggerFactory)
{
    private readonly IReadOnlyCollection<IBleFactory> _factories = factories;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    /// <summary> Enumerate all implementations for devices </summary>
    /// <returns> A list of all available devices </returns>
    public IEnumerable<IBleDevice> EnumerateDevices() => _factories
        .SelectMany(x => x.EnumerateDevices(_loggerFactory));
}