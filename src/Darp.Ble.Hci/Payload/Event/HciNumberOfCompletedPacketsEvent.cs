using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Number_Of_Completed_Packets event is used by the Controller to indicate to the Host how many HCI Data packets or HCI ISO Data packets have been completed for each Handle since the previous HCI_Number_Of_Completed_Packets event was sent to the Host </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-ca477bbf-f878-bda9-4eab-174c458d7a94"/>
[BinaryObject]
public readonly partial record struct HciNumberOfCompletedPacketsEvent : IHciEvent<HciNumberOfCompletedPacketsEvent>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_Number_Of_Completed_Packets;

    /// <summary> The number of Handles and Num_HCI_Data_Packets parameters pairs contained in this event. </summary>
    public required byte NumHandles { get; init; }
    /// <summary> Connection_Handle or BIS_Handle </summary>
    [BinaryElementCount(nameof(NumHandles))]
    public required HciNumberOfCompletedPackets[] Handles { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source,
        out HciNumberOfCompletedPacketsEvent hciEvent,
        out int bytesDecoded)
    {
        bytesDecoded = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = source.Span;
        if (span.Length < 1) return false;
        byte numHandles = span[0];
        if (span.Length < 1 + numHandles * 4) return false;
        var handles = new HciNumberOfCompletedPackets[numHandles];
        for (var i = 0; i < numHandles; i++)
        {
            int startingIndex = 1 + i * 4;
            ushort connectionHandle = BinaryPrimitives.ReadUInt16LittleEndian(span[startingIndex..]);
            ushort numCompletedPackets = BinaryPrimitives.ReadUInt16LittleEndian(span[(startingIndex + 2)..]);
            handles[i] = new HciNumberOfCompletedPackets
            {
                ConnectionHandle = connectionHandle,
                NumCompletedPackets = numCompletedPackets,
            };
        }
        bytesDecoded = 1 + numHandles * 4;
        hciEvent = new HciNumberOfCompletedPacketsEvent
        {
            NumHandles = numHandles,
            Handles = handles,
        };
        return true;
    }
}