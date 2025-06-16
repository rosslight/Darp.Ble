using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadBufferSizeCommandV1"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeReadBufferSizeResultV1 : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }

    /// <summary> The LE_ACL_Data_Packet_Length parameter shall be used to determine the maximum size of the L2CAP PDU fragments that are contained in ACL data packets, and which are transferred from the Host to the Controller to be broken up into packets by the Link Layer </summary>
    public required ushort LeAclDataPacketLength { get; init; }

    /// <summary> The Total_Num_LE_ACL_Data_Packets parameter contains the total number of HCI ACL Data packets that can be stored in the data buffers of the Controller </summary>
    public required byte TotalNumLeAclDataPackets { get; init; }
}
