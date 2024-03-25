using System.Reflection;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload;

public interface IDefaultEncodable<out TSelf> : IEncodable
    where TSelf : unmanaged, IDefaultEncodable<TSelf>
{
    TSelf GetThis();
    int IEncodable.Length => GetThis().GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance).Length == 0 ? 0 : Marshal.SizeOf<TSelf>();
    bool IEncodable.TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length)
            return false;
        if (Length == 0) return true;
        Span<TSelf> valSpan = stackalloc TSelf[1];
        valSpan[0] = GetThis();
        MemoryMarshal.Cast<TSelf, byte>(valSpan).CopyTo(destination);
        return true;
    }
}