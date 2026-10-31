using System.Diagnostics;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A discovered characteristic that supports <typeparamref name="TProp1"/> </summary>
/// <typeparam name="TProp1"> The property marker </typeparam>
public class RemoteCharacteristic<TProp1> : IRemoteCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    internal RemoteCharacteristic(RemoteGattCharacteristic characteristic)
    {
        Debug.Assert(characteristic.Properties.HasFlag(TProp1.GattProperty));
        Characteristic = characteristic;
    }

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public RemoteGattCharacteristic Characteristic { get; }
}

/// <summary> A discovered characteristic that supports <typeparamref name="TProp1"/> and <typeparamref name="TProp2"/> </summary>
/// <typeparam name="TProp1"> The first property marker </typeparam>
/// <typeparam name="TProp2"> The second property marker </typeparam>
public sealed class RemoteCharacteristic<TProp1, TProp2> : RemoteCharacteristic<TProp1>, IRemoteCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    internal RemoteCharacteristic(RemoteGattCharacteristic characteristic)
        : base(characteristic)
    {
        Debug.Assert(characteristic.Properties.HasFlag(TProp2.GattProperty));
    }
}
