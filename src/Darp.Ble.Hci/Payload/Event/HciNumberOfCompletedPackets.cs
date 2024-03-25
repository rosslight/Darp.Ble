using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciNumberOfCompletedPacketsInfo : IDefaultDecodable<HciNumberOfCompletedPacketsInfo>
{
    public required ushort ConnectionHandle { get; init; }
    public required ushort NumCompletedPackets { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciNumberOfCompletedPackets : IHciEvent<HciNumberOfCompletedPackets>
{
    public static HciEventCode EventCode => HciEventCode.HCI_Number_Of_Completed_Packets;

    public required byte NumHandles { get; init; }
    public required HciNumberOfCompletedPacketsInfo[] Handles { get; init; }

    public static bool TryDecode(in ReadOnlyMemory<byte> buffer,
        out HciNumberOfCompletedPackets hciEvent,
        out int bytesRead)
    {
        bytesRead = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = buffer.Span;
        if (span.Length < 1) return false;
        byte numHandles = span[0];
        if (span.Length < 1 + numHandles * 4) return false;
        var handles = new HciNumberOfCompletedPacketsInfo[numHandles];
        for (var i = 0; i < numHandles; i++)
        {
            int startingIndex = 1 + i * 4;
            ushort connectionHandle = BinaryPrimitives.ReadUInt16LittleEndian(span[startingIndex..]);
            ushort numCompletedPackets = BinaryPrimitives.ReadUInt16LittleEndian(span[(startingIndex + 2)..]);
            handles[i] = new HciNumberOfCompletedPacketsInfo
            {
                ConnectionHandle = connectionHandle,
                NumCompletedPackets = numCompletedPackets
            };
        }
        bytesRead = 1 + numHandles * 4;
        hciEvent = new HciNumberOfCompletedPackets
        {
            NumHandles = numHandles,
            Handles = handles
        };
        return true;
    }
}