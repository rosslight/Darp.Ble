using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Transport;
using Darp.Ble.HciHost.Usb;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for all available hci devices </summary>
public sealed class SerialHciHostBleFactory : IHciHostBleFactory
{
    /// <inheritdoc />
    public BleAddress? RandomAddress { get; set; }

    /// <summary> A simple mapping of vendorId and productId to the name of the device </summary>
    public IDictionary<(ushort VendorId, ushort ProductId), string> DeviceNameMapping { get; } =
        new Dictionary<(ushort VendorId, ushort ProductId), string> { [(0x2FE3, 0x0004)] = "nrf52840 dongle" };

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary> Settings to be used by devices enumerated by this factory </summary>
    public HciSettings Settings { get; set; } = HciSettings.Default;

    IEnumerable<IBleDevice> IBleFactory.EnumerateDevices(IServiceProvider serviceProvider)
    {
        // Using vendorId of NordicSemiconductor and productId self defined
        foreach (UsbPortInfo portInfo in UsbPort.GetPortInfos())
        {
            if (portInfo.Port is null)
                continue;
            if (!DeviceNameMapping.TryGetValue((portInfo.VendorId, portInfo.ProductId), out string? deviceName))
                continue;

#pragma warning disable CA2000 // Dispose objects before losing scope -> False positive
            var transportLayer = new H4TransportLayer(portInfo.Port, serviceProvider.GetLogger<H4TransportLayer>());
#pragma warning restore CA2000
            yield return new HciHostBleDevice(
                $"{deviceName} ({portInfo.Port})",
                randomAddress: RandomAddress,
                transportLayer: transportLayer,
                settings: Settings,
                serviceProvider: serviceProvider
            );
        }
    }
}
