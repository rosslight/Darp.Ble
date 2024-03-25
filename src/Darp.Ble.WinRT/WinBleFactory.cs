using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public sealed class WinBleFactory : IPlatformSpecificBleFactory
{
    public IEnumerable<IPlatformSpecificBleDevice> EnumerateDevices()
    {
        yield return new WinBleDevice();
    }
}