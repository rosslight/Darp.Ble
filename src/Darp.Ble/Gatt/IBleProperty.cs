using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The ble property </summary>
public interface IBleProperty
{
    /// <summary> The GattProperty </summary>
    static abstract GattProperty GattProperty { get; }
}