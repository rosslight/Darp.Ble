using Darp.Ble.Data;
using Darp.Ble.Hci;

namespace Darp.Ble.HciHost;

/// <summary> Options of the HCI adapters found on USB serial ports </summary>
public sealed class SerialHciHostOptions
{
    /// <summary> The random address every adapter starts with; a new random static address per adapter when null </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <summary> Timeouts of the HCI layer </summary>
    public HciSettings Settings { get; set; } = HciSettings.Default;

    /// <summary> Whether the adapter serves the GATT service with Service Changed and Database Hash; true by default </summary>
    /// <remarks> Without it, centrals cannot learn that services changed, as with devices that do not serve it </remarks>
    public bool IncludeGattService { get; set; } = true;

    /// <summary> The USB devices that are HCI controllers, by vendor and product id, with the name their adapters get </summary>
    public IDictionary<(ushort VendorId, ushort ProductId), string> DeviceNameMapping { get; } =
        new Dictionary<(ushort VendorId, ushort ProductId), string> { [(0x2FE3, 0x0004)] = "nrf52840 dongle" };
}
