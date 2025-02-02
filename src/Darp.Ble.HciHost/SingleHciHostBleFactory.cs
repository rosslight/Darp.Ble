using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for a single, specified hci host </summary>
/// <param name="portName"> The port name to be scanned for a hci host </param>
public sealed class SingleHciHostBleFactory(string portName) : IBleFactory
{
    /// <summary> A hardcoded serial port </summary>
    public string PortName { get; } = portName;

    /// <summary> A hardcoded device name </summary>
    public string? DeviceName { get; set; }

    /// <summary> The random address of the device </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILoggerFactory loggerFactory)
    {
        yield return new HciHostBleDevice(
            PortName,
            DeviceName ?? PortName,
            randomAddress: RandomAddress,
            loggerFactory
        );
    }
}
