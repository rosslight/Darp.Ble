using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public sealed class WinBleFactory : IBleFactory
{
    public IEnumerable<IBleDeviceImplementation> EnumerateDevices()
    {
        yield return new WinBleDevice();
    }
}