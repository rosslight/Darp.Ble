using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IBleFactory
{
    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILogger? logger)
    {
        yield return new WinBleDevice(logger);
    }
}