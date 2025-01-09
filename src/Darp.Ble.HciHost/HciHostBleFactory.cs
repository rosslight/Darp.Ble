using Darp.Ble.Data;
using Darp.Ble.HciHost.Usb;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> Search for the default windows ble device </summary>
public sealed class HciHostBleFactory : IBleFactory
{
    private readonly string? _port;
    private readonly BleAddress? _randomAddress;

    /// <summary> A simple mapping of vendorId and productId to the name of the device </summary>
    public IDictionary<(ushort VendorId, ushort ProductId), string> DeviceNameMapping { get; } =
        new Dictionary<(ushort VendorId, ushort ProductId), string>
        {
            [(0x2FE3, 0x0004)] = "nrf52840 dongle",
        };

    /// <summary> Initialize a new BleFactory which will enumerate all Ports </summary>
    public HciHostBleFactory()
    {
    }

    /// <summary> Initialize a new BleFactory with a set port </summary>
    /// <param name="port"> The port to be enumerated </param>
    /// <param name="randomAddress"> The random address the device should use </param>
    public HciHostBleFactory(string port, BleAddress? randomAddress)
    {
        _port = port;
        _randomAddress = randomAddress;
    }

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILogger? logger)
    {
        if (_port is not null)
        {
            yield return new HciHostBleDevice(_port, _port, randomAddress: null, logger);
            yield break;
        }

        // Using vendorId of NordicSemiconductor and productId self defined
        foreach (UsbPortInfo portInfo in UsbPort.GetPortInfos())
        {
            if (portInfo.Port is null) continue;
            if (!DeviceNameMapping.TryGetValue((portInfo.VendorId, portInfo.ProductId), out string? deviceName))
                continue;
            yield return new HciHostBleDevice(portInfo.Port, $"{deviceName} ({portInfo.Port})", randomAddress: null, logger);
        }
    }
}

