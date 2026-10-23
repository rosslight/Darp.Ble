namespace Darp.Ble.Gatt;

/// <summary> A hosted characteristic that supports <typeparamref name="TProp1"/> and whose value is a <typeparamref name="T"/> </summary>
/// <remarks> Typed characteristics do not implement the untyped marker interfaces, so C# can infer <typeparamref name="T"/> for their operations </remarks>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProp1"> The property marker </typeparam>
public interface ITypedLocalCharacteristic<T, TProp1> : ILocalCharacteristic
    where TProp1 : IBleProperty
{
    /// <summary> Encodes and decodes the value </summary>
    IGattTypedCharacteristic<T> Declaration { get; }
}

/// <summary> A hosted characteristic that supports <typeparamref name="TProp1"/> and <typeparamref name="TProp2"/> and whose value is a <typeparamref name="T"/> </summary>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProp1"> The first property marker </typeparam>
/// <typeparam name="TProp2"> The second property marker </typeparam>
public interface ITypedLocalCharacteristic<T, TProp1, TProp2> : ILocalCharacteristic
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> Encodes and decodes the value </summary>
    IGattTypedCharacteristic<T> Declaration { get; }
}
