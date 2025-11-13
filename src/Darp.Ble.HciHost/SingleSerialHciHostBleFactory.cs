using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for a single, specified hci host </summary>
/// <param name="portName"> The port name to be scanned for a hci host </param>
public sealed class SingleSerialHciHostBleFactory(string portName) : IHciHostBleFactory
{
    /// <summary> A hardcoded serial port </summary>
    public string PortName { get; } = portName;

    /// <summary> A hardcoded device name </summary>
    public string? DeviceName { get; set; }

    /// <summary> The random address of the device </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary> Settings to be used by devices enumerated by this factory </summary>
    public HciSettings Settings { get; set; } = HciSettings.Default;

    /// <inheritdoc />
    IEnumerable<IBleDevice> IBleFactory.EnumerateDevices(IServiceProvider serviceProvider)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope -> False positive
        var transportLayer = new H4TransportLayer(PortName, serviceProvider.GetLogger<H4TransportLayer>());
#pragma warning restore CA2000
        yield return new HciHostBleDevice(
            DeviceName ?? PortName,
            randomAddress: RandomAddress,
            transportLayer: transportLayer,
            settings: Settings,
            serviceProvider: serviceProvider
        );
    }
}
