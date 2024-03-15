namespace Darp.Ble.Data;

public static class ByteArrayExtensions
{
    public static byte[] ToByteArray(this string hexString)
    {
        var bytes = new byte[hexString.Length >> 1];
        hexString.ToByteArray(bytes);
        return bytes;
    }

    public static void ToByteArray(this string hexString, in Span<byte> outSpan)
    {
        if (hexString.Length % 2 == 1)
            throw new Exception("The binary string cannot have an odd number of digits");
        if (outSpan.Length < hexString.Length >> 1)
            throw new Exception($"Buffer is not bug enough. Expected at least {hexString.Length}, but got {outSpan.Length}");
        for (var i = 0; i < hexString.Length >> 1; ++i)
            outSpan[i] = (byte)((GetHexVal(hexString[i << 1]) << 4) + GetHexVal(hexString[(i << 1) + 1]));
    }

    public static string ToHexString(this byte[] bytes) => ((ReadOnlySpan<byte>)bytes).ToHexString();
    public static string ToHexString(this in ReadOnlyMemory<byte> memory) => memory.Span.ToHexString();
    public static string ToHexString(this in ReadOnlySpan<byte> span) => Convert.ToHexString(span);

    private static int GetHexVal(char hex)
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