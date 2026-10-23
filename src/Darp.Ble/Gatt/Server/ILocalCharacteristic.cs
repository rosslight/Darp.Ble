using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A hosted characteristic seen through a declaration; its operations are extension methods per property marker </summary>
public interface ILocalCharacteristic
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    GattProperty Properties { get; }

    /// <summary> The untyped characteristic with every operation </summary>
    LocalGattCharacteristic Characteristic { get; }
}

/// <summary> A hosted characteristic that supports the property <typeparamref name="TProp1"/> </summary>
/// <typeparam name="TProp1"> The property marker </typeparam>
public interface ILocalCharacteristic<TProp1> : ILocalCharacteristic
    where TProp1 : IBleProperty;
