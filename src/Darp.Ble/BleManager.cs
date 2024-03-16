using System.Reactive;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> The base manager class. Holds all implementations </summary>
public sealed class BleManager
{
    private readonly IReadOnlyCollection<IBleFactory> _factories;
    private readonly IObserver<(BleDevice, LogEvent)>? _logObserver;

    internal BleManager(IReadOnlyCollection<IBleFactory> factories,
        IReadOnlyCollection<Action<BleDevice, LogEvent>> logActions)
    {
        _factories = factories;
        if (logActions.Count > 0)
        {
            _logObserver = Observer.Create<(BleDevice Device, LogEvent Event)>(next =>
            {
                foreach (Action<BleDevice, LogEvent> logAction in logActions)
                {
                    logAction(next.Device, next.Event);
                }
            });
        }
    }

    /// <summary> Enumerate all implementations for devices </summary>
    /// <returns> A list of all available devices </returns>
    public IEnumerable<BleDevice> EnumerateDevices() => _factories
        .SelectMany(x => x.EnumerateAdapters())
        .Select(x => new BleDevice(x, _logObserver));
}