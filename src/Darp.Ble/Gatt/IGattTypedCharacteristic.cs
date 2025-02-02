namespace Darp.Ble.Gatt;

/// <summary> A gatt attribute declaration with a value of a specific type </summary>
/// <typeparam name="T"> The type of the attribute value </typeparam>
public interface IGattTypedCharacteristic<T>
{
    /// <summary> A delegate specifying how to read a value </summary>
    public delegate T ReadValueFunc(ReadOnlySpan<byte> source);

    /// <summary> A delegate specifying how to write a value </summary>
    public delegate byte[] WriteValueFunc(T value);

    /// <summary> Read the value from a given source of bytes </summary>
    /// <param name="source"> The source to read from </param>
    /// <returns> The value </returns>
    T ReadValue(ReadOnlySpan<byte> source);

    /// <summary> Write a specific value into a destination of bytes </summary>
    /// <param name="value"> The value to write </param>
    /// <returns> The byte array </returns>
    byte[] WriteValue(T value);
}
