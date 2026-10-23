using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A hosted characteristic that supports <typeparamref name="TProp1"/> </summary>
/// <typeparam name="TProp1"> The property marker </typeparam>
public class LocalCharacteristic<TProp1> : ILocalCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    internal LocalCharacteristic(LocalGattCharacteristic characteristic)
    {
        Characteristic = characteristic;
    }

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public LocalGattCharacteristic Characteristic { get; }
}

/// <summary> A hosted characteristic that supports <typeparamref name="TProp1"/> and <typeparamref name="TProp2"/> </summary>
/// <typeparam name="TProp1"> The first property marker </typeparam>
/// <typeparam name="TProp2"> The second property marker </typeparam>
public sealed class LocalCharacteristic<TProp1, TProp2> : LocalCharacteristic<TProp1>, ILocalCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    internal LocalCharacteristic(LocalGattCharacteristic characteristic)
        : base(characteristic) { }
}
