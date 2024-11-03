using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> A default decodable which assumes the inheriting object is a blittable struct </summary>
/// <typeparam name="TSelf"> The type of the encodable </typeparam>
public interface IDefaultDecodable<TSelf> : IDecodable<TSelf>
    where TSelf : unmanaged, IDefaultDecodable<TSelf>
{
    /// <inheritdoc />
#pragma warning disable CA1033
    static bool IDecodable<TSelf>.TryDecode(in ReadOnlyMemory<byte> source,
        out TSelf result,
        out int bytesRead)
#pragma warning restore CA1033
    {
        bytesRead = Marshal.SizeOf<TSelf>();
        if (source.Length < bytesRead)
        {
            result = default;
            return false;
        }
        result = ToStructUnsafe<TSelf>(source);
        return true;
    }

    private static T ToStructUnsafe<T>(in ReadOnlyMemory<byte> memory) where T : unmanaged
    {
        using MemoryHandle memoryHandle = memory.Pin();
        unsafe
        {
            return Unsafe.Read<T>(memoryHandle.Pointer);
        }
    }
}