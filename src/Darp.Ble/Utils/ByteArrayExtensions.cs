namespace Darp.Ble.Utils;

/// <summary> Extensions for byte arrays </summary>
public static class ByteArrayExtensions
{
    /// <summary> Create a byte array from a given hex string </summary>
    /// <param name="hexString"> The hex string with two chars per byte </param>
    /// <returns> The byte array </returns>
    /// <exception cref="ArgumentException"> Thrown if string is in wrong format </exception>
    public static byte[] ToByteArray(this string hexString)
    {
        var bytes = new byte[hexString.Length >> 1];
        hexString.WriteByteArray(bytes);
        return bytes;
    }

    /// <summary>
    /// Writes a given hex string into a span
    /// </summary>
    /// <param name="hexString"> The hex string with two chars per byte </param>
    /// <param name="destination"> The destination to write the bytes to </param>
    /// <exception cref="ArgumentException"> Thrown if string is in wrong format or destination is not big enough </exception>
    public static void WriteByteArray(this string hexString, in Span<byte> destination)
    {
        if (hexString.Length % 2 == 1)
            throw new ArgumentException("The binary string cannot have an odd number of digits", nameof(hexString));
        if (destination.Length < hexString.Length >> 1)
            throw new ArgumentException($"Buffer is not bug enough. Expected at least {hexString.Length}, but got {destination.Length}", nameof(destination));
        for (var i = 0; i < hexString.Length >> 1; ++i)
            destination[i] = (byte)((GetHexVal(hexString[i << 1]) << 4) + GetHexVal(hexString[(i << 1) + 1]));
    }

    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="bytes"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this byte[] bytes) => ((ReadOnlySpan<byte>)bytes).ToHexString();
    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="memory"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this in ReadOnlyMemory<byte> memory) => memory.Span.ToHexString();
    /// <summary> Create a hex string from a given array of bytes </summary>
    /// <param name="span"> The bytes to be converted </param>
    /// <returns> The hex string with two chars byte </returns>
    public static string ToHexString(this in ReadOnlySpan<byte> span) => Convert.ToHexString(span);

    public static byte GetHexVal(this in ReadOnlySpan<char> s) => (byte)((GetHexVal(s[0]) << 4) + GetHexVal(s[1]));

    public static int GetHexVal(char hex)
    {
        int val = hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : val < 97 ? 55 : 87);
    }
}