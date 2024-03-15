using Darp.Ble.Implementation;

namespace Darp.Ble;

/// <summary> The base manager class. Holds all implementations </summary>
public sealed class BleManager
{
    private readonly IEnumerable<IBleImplementation> _implementations;

    internal BleManager(IEnumerable<IBleImplementation> implementations)
    {
        _implementations = implementations;
    }

    /// <summary> Enumerate all implementations for devices </summary>
    /// <returns> A list of all available devices </returns>
    public IEnumerable<BleDevice> EnumerateDevices()
    {
        return _implementations.SelectMany(x => x.EnumerateAdapters());
    }
}