namespace Darp.Ble.Tests;

/// <summary> Extensions for byte arrays </summary>
public static class ByteArrayExtensions
{
    /// <summary> Create a byte array from a given hex string </summary>
    /// <param name="hexString"> The hex string with two chars per byte </param>
    /// <returns> The byte array </returns>
    /// <exception cref="ArgumentException"> Thrown if string is in wrong format </exception>
    public static byte[] ToByteArray(this string hexString) => Convert.FromHexString(hexString);

    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="span"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this in ReadOnlySpan<byte> span) => Convert.ToHexString(span);

    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="bytes"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this byte[] bytes) =>
        ((ReadOnlySpan<byte>)bytes).ToHexString();

    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="memory"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this in ReadOnlyMemory<byte> memory) =>
        memory.Span.ToHexString();
}
