using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciCommandCompleteEvent<TParameters> : IHciEvent<HciCommandCompleteEvent<TParameters>>
    where TParameters : IDecodable<TParameters>
{
    public static HciEventCode EventCode => HciEventCode.HCI_Command_Complete;

    public required byte NumHciCommandPackets { get; init; }
    public required HciOpCode CommandOpCode { get; init; }
    public required TParameters ReturnParameters { get; init; }

    static bool IDecodable<HciCommandCompleteEvent<TParameters>>.TryDecode(in ReadOnlyMemory<byte> buffer,
        out HciCommandCompleteEvent<TParameters> hciEvent,
        out int bytesRead)
    {
        bytesRead = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = buffer.Span;
        byte numHciCommandPackets = span[0];
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(span[1..], out ushort commandOpCode))
            return false;
        if (!TParameters.TryDecode(buffer[3..], out TParameters? returnParameters, out int parameterBytesRead))
            return false;
        bytesRead = 3 + parameterBytesRead;
        hciEvent = new HciCommandCompleteEvent<TParameters>
        {
            NumHciCommandPackets = numHciCommandPackets,
            CommandOpCode = (HciOpCode)commandOpCode,
            ReturnParameters = returnParameters
        };
        return true;
    }
}