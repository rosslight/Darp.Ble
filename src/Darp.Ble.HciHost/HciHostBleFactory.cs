using Darp.Ble.Implementation;

namespace Darp.Ble.HciHost;

/// <summary> Search for the default windows ble device </summary>
public sealed class HciHostBleFactory(string port) : IPlatformSpecificBleFactory
{
    private readonly string _port = port;

    /// <inheritdoc />
    public IEnumerable<IPlatformSpecificBleDevice> EnumerateDevices()
    {
        yield return new HciHostBleDevice(_port);
    }
}