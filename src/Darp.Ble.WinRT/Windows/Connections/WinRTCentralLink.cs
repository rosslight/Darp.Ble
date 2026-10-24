using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.WinRT.Connections;

/// <summary> A central's connection to this app's services: one per device for as long as its sessions last </summary>
internal sealed class WinRTCentralLink(string deviceId, BleAddress address) : ICentralConnectionDriver
{
    /// <summary> Windows' id of the device; opaque </summary>
    public string DeviceId { get; } = deviceId;

    public BleAddress Address { get; } = address;
}
