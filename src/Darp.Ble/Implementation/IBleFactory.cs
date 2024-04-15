using Darp.Ble.Logger;

namespace Darp.Ble.Implementation;

/// <summary> The ble implementation config </summary>
public interface IBleFactory
{
    /// <summary> Enumerate all adapters which can be found by the implementation </summary>
    /// <param name="logger"> An observable for logs </param>
    /// <returns> All implementation specific ble devices </returns>
    IEnumerable<IBleDevice> EnumerateDevices(IObserver<(BleDevice, LogEvent)>? logger);
}