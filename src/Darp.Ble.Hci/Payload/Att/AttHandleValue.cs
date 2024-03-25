using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.7.1 ATT_HANDLE_VALUE_NTF
/// </summary>
public readonly struct AttHandleValueNtf : IAttPdu, IDecodable<AttHandleValueNtf>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_HANDLE_VALUE_NTF;
    public required AttOpCode OpCode { get; init; }
    public required ushort Handle { get; init; }
    public required byte[] Value { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttHandleValueNtf result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 3) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        ushort handle = BinaryPrimitives.ReadUInt16LittleEndian(span[1..]);
        result = new AttHandleValueNtf
        {
            OpCode = opCode,
            Handle = handle,
            Value = span[3..].ToArray()
        };
        return true;
    }
}