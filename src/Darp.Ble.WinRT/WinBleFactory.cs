using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IPlatformSpecificBleFactory
{
    /// <inheritdoc />
#pragma warning disable CA1822
    public IEnumerable<IPlatformSpecificBleDevice> EnumerateDevices()
#pragma warning restore CA1822
    {
        yield return new WinBleDevice();
    }
}