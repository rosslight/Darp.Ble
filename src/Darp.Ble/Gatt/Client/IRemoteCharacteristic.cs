using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A discovered characteristic seen through a declaration; its operations are extension methods per property marker </summary>
public interface IRemoteCharacteristic
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> What the characteristic supports; at least what the declaration asks for </summary>
    GattProperty Properties { get; }

    /// <summary> The untyped characteristic with every operation </summary>
    RemoteGattCharacteristic Characteristic { get; }
}

/// <summary> A discovered characteristic that supports the property <typeparamref name="TProp1"/> </summary>
/// <typeparam name="TProp1"> The property marker </typeparam>
public interface IRemoteCharacteristic<TProp1> : IRemoteCharacteristic
    where TProp1 : IBleProperty;
