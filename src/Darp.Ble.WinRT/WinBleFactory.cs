namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IBleFactory
{
    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(IServiceProvider serviceProvider)
    {
        yield return new WinBleDevice(serviceProvider);
    }
}
