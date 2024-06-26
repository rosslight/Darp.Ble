using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> The ble implementation config </summary>
public interface IBleFactory
{
    /// <summary> Enumerate all adapters which can be found by the implementation </summary>
    /// <param name="logger"> An observable for logs </param>
    /// <returns> All implementation specific ble devices </returns>
    IEnumerable<IBleDevice> EnumerateDevices(ILogger? logger);
}