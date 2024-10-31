using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic with a single property </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
public sealed class Characteristic<TProp1>(BleUuid uuid) where TProp1 : IBleProperty
{
    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; } = uuid;
    /// <summary> The property </summary>
    public GattProperty Property => TProp1.GattProperty;
    /// <summary> Initialize a new characteristic from a given ushort </summary>
    /// <param name="uuid"> The UUID as ushort </param>
    public Characteristic(ushort uuid) : this(new BleUuid(uuid)) {}
}