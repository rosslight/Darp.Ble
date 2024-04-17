using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Event;

public interface IDecodable<TSelf> where TSelf : IDecodable<TSelf>
{
    static abstract bool TryDecode(in ReadOnlyMemory<byte> source,
        [NotNullWhen(true)] out TSelf? result,
        out int bytesDecoded);
    public static bool TryReadUInt8(ReadOnlySpan<byte> source, out byte value)
    {
        if (source.Length == 0)
        {
            value = default;
            return false;
        }
        value = source[0];
        return true;
    }
    public static bool TryReadInt8(ReadOnlySpan<byte> source, out sbyte value)
    {
        if (source.Length == 0)
        {
            value = default;
            return false;
        }
        value = (sbyte)source[0];
        return true;
    }
}