using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload;

/// <summary> A default encodable which assumes the inheriting object is a blittable struct </summary>
/// <typeparam name="TSelf"> The type of the encodable </typeparam>
[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types")]
public interface IDefaultEncodable<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] out TSelf
> : IEncodable
    where TSelf : unmanaged, IDefaultEncodable<TSelf>
{
    int IEncodable.Length => typeof(TSelf)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance).Length == 0 ? 0 : Marshal.SizeOf<TSelf>();
    bool IEncodable.TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length)
            return false;
        if (Length == 0) return true;
        Span<TSelf> valSpan = stackalloc TSelf[1];
        valSpan[0] = (TSelf)this;
        MemoryMarshal.Cast<TSelf, byte>(valSpan).CopyTo(destination);
        return true;
    }
}