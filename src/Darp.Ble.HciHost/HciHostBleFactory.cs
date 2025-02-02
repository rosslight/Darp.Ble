using Darp.Ble.Data;
using Darp.Ble.HciHost.Usb;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for all available hci devices </summary>
public sealed class HciHostBleFactory : IBleFactory
{
    /// <summary> The random address of the device </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <summary> A simple mapping of vendorId and productId to the name of the device </summary>
    public IDictionary<(ushort VendorId, ushort ProductId), string> DeviceNameMapping { get; } =
        new Dictionary<(ushort VendorId, ushort ProductId), string>
        {
            [(0x2FE3, 0x0004)] = "nrf52840 dongle",
        };

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILoggerFactory loggerFactory)
    {
        // Using vendorId of NordicSemiconductor and productId self defined
        foreach (UsbPortInfo portInfo in UsbPort.GetPortInfos())
        {
            if (portInfo.Port is null)
                continue;
            if (
                !DeviceNameMapping.TryGetValue(
                    (portInfo.VendorId, portInfo.ProductId),
                    out string? deviceName
                )
            )
                continue;
            yield return new HciHostBleDevice(
                portInfo.Port,
                $"{deviceName} ({portInfo.Port})",
                randomAddress: RandomAddress,
                loggerFactory
            );
        }
    }
}
