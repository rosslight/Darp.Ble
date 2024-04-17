using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

public interface IDefaultDecodable<TSelf> : IDecodable<TSelf>
    where TSelf : unmanaged, IDefaultDecodable<TSelf>
{
    static bool IDecodable<TSelf>.TryDecode(in ReadOnlyMemory<byte> source,
        out TSelf result,
        out int bytesRead)
    {
        bytesRead = Marshal.SizeOf<TSelf>();
        if (source.Length < bytesRead)
        {
            result = default;
            return false;
        }
        result = source.ToStructUnsafe<TSelf>();
        return true;
    }
}