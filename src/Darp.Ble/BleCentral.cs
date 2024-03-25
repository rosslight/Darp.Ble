using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> The central view of a ble device </summary>
public sealed class BleCentral
{
    private readonly IObserver<LogEvent>? _logger;

    /// <summary> The ble device </summary>
    public BleDevice Device { get; }

    internal BleCentral(BleDevice device, object central, IObserver<LogEvent>? logger)
    {
        _logger = logger;
        Device = device;
    }
}