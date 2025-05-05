using Darp.Ble.Data;

namespace Darp.Ble.HciHost;

/// <summary> The hci host ble factory </summary>
public interface IHciHostBleFactory : IBleFactory
{
    /// <summary> The random address of the device </summary>
    BleAddress? RandomAddress { get; set; }

    /// <summary> The timeProvider to be used </summary>
    TimeProvider TimeProvider { get; set; }
}
