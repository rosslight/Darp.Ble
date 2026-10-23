namespace Darp.Ble.Gatt;

/// <summary> The characteristic declaration </summary>
public interface ICharacteristicDeclaration : IGattDeclaration
{
    /// <summary> Properties that are part of the characteristic declaration </summary>
    GattProperty Properties { get; }
}

/// <summary> The characteristic declaration with specified properties </summary>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public interface ICharacteristicDeclaration<TProp1> : ICharacteristicDeclaration
    where TProp1 : IBleProperty;
