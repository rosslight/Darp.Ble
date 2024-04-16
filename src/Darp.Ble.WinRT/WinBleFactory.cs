using Darp.Ble.Logger;

namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IBleFactory
{
    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(IObserver<(BleDevice, LogEvent)>? logger)
    {
        yield return new WinBleDevice(logger);
    }
}