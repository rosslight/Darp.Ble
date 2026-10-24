using Darp.Ble.Data;
using Darp.Ble.Hci;

namespace Darp.Ble.HciHost;

/// <summary> Options of one HCI adapter, e.g. on a given serial port or transport </summary>
public sealed class HciHostOptions
{
    /// <summary> The adapter's name, shown in <see cref="BleAdapterInfo.Name"/>; the port name when null </summary>
    public string? Name { get; set; }

    /// <summary> The random address the adapter starts with; a new random static address when null </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <summary> Timeouts of the HCI layer </summary>
    public HciSettings Settings { get; set; } = HciSettings.Default;

    /// <summary> Whether the adapter serves the GATT service with Service Changed and Database Hash; true by default </summary>
    /// <remarks> Without it, centrals cannot learn that services changed, as with devices that do not serve it </remarks>
    public bool IncludeGattService { get; set; } = true;
}
