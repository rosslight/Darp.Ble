using Darp.Ble.Data;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for a single, specified hci host </summary>
/// <param name="portName"> The port name to be scanned for a hci host </param>
public sealed class SingleHciHostBleFactory(string portName) : IHciHostBleFactory
{
    /// <summary> A hardcoded serial port </summary>
    public string PortName { get; } = portName;

    /// <summary> A hardcoded device name </summary>
    public string? DeviceName { get; set; }

    /// <summary> The random address of the device </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <inheritdoc />
    IEnumerable<IBleDevice> IBleFactory.EnumerateDevices(IServiceProvider serviceProvider)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope -> False positive
        var transportLayer = new H4TransportLayer(PortName, serviceProvider.GetLogger<H4TransportLayer>());
#pragma warning restore CA2000
        yield return new HciHostBleDevice(
            DeviceName ?? PortName,
            randomAddress: RandomAddress,
            transportLayer,
            serviceProvider
        );
    }
}

#pragma warning disable MA0048
/// <summary> Delegate which describes configuration using a broadcaster and a peripheral </summary>
/// <param name="bleDevice"> The mocked bleDevice </param>
public delegate Task InitializeSimpleAsync(IBleDevice bleDevice);

/// <summary> Delegate which describes configuration using a broadcaster and a peripheral </summary>
/// <param name="bleDevice"> The mocked bleDevice </param>
/// <param name="deviceSettings"> Settings specific to the mock device </param>
public delegate Task InitializeAsync(IBleDevice bleDevice, string deviceSettings); //, MockDeviceSettings deviceSettings);
