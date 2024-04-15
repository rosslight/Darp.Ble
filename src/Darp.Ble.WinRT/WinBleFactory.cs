using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IBleFactory
{
    /// <inheritdoc />
#pragma warning disable CA1822
    public IEnumerable<IBleDevice> EnumerateDevices(IObserver<(BleDevice, LogEvent)>? logObserver)
#pragma warning restore CA1822
    {
        yield return new WinBleDevice();
    }
}