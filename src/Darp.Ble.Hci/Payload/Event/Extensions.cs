using System.Buffers;
using System.Runtime.CompilerServices;

namespace Darp.Ble.Hci.Payload.Event;

public static class Extensions
{
    public static T ToStructUnsafe<T>(this in ReadOnlyMemory<byte> memory) where T : unmanaged
    {
        using MemoryHandle memoryHandle = memory.Pin();
        unsafe
        {
            return Unsafe.Read<T>(memoryHandle.Pointer);
        }
    }
}