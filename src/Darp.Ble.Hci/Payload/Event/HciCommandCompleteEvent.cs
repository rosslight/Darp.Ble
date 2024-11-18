using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Command_Complete event is used by the Controller for most commands to transmit return status of a command and the other event parameters that are specified for the issued HCI command </summary>
/// <typeparam name="TParameters"></typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciCommandCompleteEvent<TParameters> : IHciEvent<HciCommandCompleteEvent<TParameters>>
    where TParameters : IDecodable<TParameters>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_Command_Complete;

    /// <summary> The Number of HCI Command packets which are allowed to be sent to the Controller from the Host. </summary>
    public required byte NumHciCommandPackets { get; init; }
    /// <summary> The Command_Opcode </summary>
    public required HciOpCode CommandOpCode { get; init; }
    /// <summary> This is the return parameter(s) for the command specified in the Command_Opcode event parameter. See each command’s definition for the list of return parameters associated with that command </summary>
    public required TParameters ReturnParameters { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source,
        out HciCommandCompleteEvent<TParameters> hciEvent,
        out int bytesDecoded)
    {
        bytesDecoded = default;
        hciEvent = default;
        if (source.Length < 3)
            return false;
        ReadOnlySpan<byte> span = source.Span;
        byte numHciCommandPackets = span[0];
        ushort commandOpCode = BinaryPrimitives.ReadUInt16LittleEndian(span[1..]);
        if (!TParameters.TryDecode(source[3..], out TParameters? returnParameters, out int parameterBytesRead))
            return false;
        bytesDecoded = 3 + parameterBytesRead;
        hciEvent = new HciCommandCompleteEvent<TParameters>
        {
            NumHciCommandPackets = numHciCommandPackets,
            CommandOpCode = (HciOpCode)commandOpCode,
            ReturnParameters = returnParameters,
        };
        return true;
    }
}