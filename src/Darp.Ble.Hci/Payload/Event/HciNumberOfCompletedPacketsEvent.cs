using System.Buffers.Binary;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Number_Of_Completed_Packets event is used by the Controller to indicate to the Host how many HCI Data packets or HCI ISO Data packets have been completed for each Handle since the previous HCI_Number_Of_Completed_Packets event was sent to the Host </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-ca477bbf-f878-bda9-4eab-174c458d7a94"/>
public readonly record struct HciNumberOfCompletedPacketsEvent : IHciEvent<HciNumberOfCompletedPacketsEvent>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_Number_Of_Completed_Packets;

    /// <summary> The number of Handles and Num_HCI_Data_Packets parameters pairs contained in this event. </summary>
    public required byte NumHandles { get; init; }

    /// <summary> Connection_Handle or BIS_Handle </summary>
    [BinaryElementCount(nameof(NumHandles))]
    public required HciNumberOfCompletedPackets[] Handles { get; init; }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciNumberOfCompletedPacketsEvent value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(
        ReadOnlySpan<byte> source,
        out HciNumberOfCompletedPacketsEvent value,
        out int bytesRead
    )
    {
        bytesRead = 0;
        value = default;
        if (source.Length < 1)
            return false;
        byte numHandles = source[0];
        if (source.Length < 1 + (numHandles * 4))
            return false;
        var handles = new HciNumberOfCompletedPackets[numHandles];
        for (var i = 0; i < numHandles; i++)
        {
            int startingIndex = 1 + (i * 4);
            ushort connectionHandle = BinaryPrimitives.ReadUInt16LittleEndian(source[startingIndex..]);
            ushort numCompletedPackets = BinaryPrimitives.ReadUInt16LittleEndian(source[(startingIndex + 2)..]);
            handles[i] = new HciNumberOfCompletedPackets
            {
                ConnectionHandle = connectionHandle,
                NumCompletedPackets = numCompletedPackets,
            };
        }
        bytesRead = 1 + numHandles * 4;
        value = new HciNumberOfCompletedPacketsEvent { NumHandles = numHandles, Handles = handles };
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciNumberOfCompletedPacketsEvent value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(
        ReadOnlySpan<byte> source,
        out HciNumberOfCompletedPacketsEvent value,
        out int bytesRead
    )
    {
        throw new NotSupportedException();
    }
}
