namespace Darp.Ble.Gatt;

/// <summary> A typed characteristic declaration </summary>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public interface ITypedCharacteristicDeclaration<T, TProp1> : ICharacteristicDeclaration, IGattTypedCharacteristic<T>
    where TProp1 : IBleProperty;
