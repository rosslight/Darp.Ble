namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
public sealed class WinBleFactory : IBleFactory
{
    /// <summary> The name of the resulting device </summary>
    public string Name { get; set; } = "Windows";

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(IServiceProvider serviceProvider)
    {
        yield return new WinBleDevice(serviceProvider, Name);
    }
}
