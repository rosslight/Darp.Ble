using Darp.Ble.HciHost.Usb;
using Darp.Ble.Implementation;

namespace Darp.Ble.HciHost;

/// <summary> Search for the default windows ble device </summary>
public sealed class HciHostBleFactory : IPlatformSpecificBleFactory
{
    private readonly string? _port;

    /// <summary> Initialize a new BleFactory which will enumerate all Ports </summary>
    public HciHostBleFactory()
    {
    }

    /// <summary> Initialize a new BleFactory with a set port </summary>
    /// <param name="port"> The port to be enumerated </param>
    public HciHostBleFactory(string port)
    {
        _port = port;
    }

    /// <inheritdoc />
    public IEnumerable<IPlatformSpecificBleDevice> EnumerateDevices()
    {
        if (_port is not null)
        {
            yield return new HciHostBleDevice(_port);
            yield break;
        }

        // Using vendorId of NordicSemiconductor and productId self defined
        foreach (UsbPortInfo portInfo in UsbPort.GetPortInfos()
                     .Where(x => x.VendorId is 0x2FE3 && x.ProductId is 0x0004))
        {
            if (portInfo.Port is null) continue;
            yield return new HciHostBleDevice(portInfo.Port);
        }
    }
}

