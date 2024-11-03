using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> A server can send a notification of an attribute’s value at any time </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-40393db4-55e7-1a22-5eff-2bbcce21de5d"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttHandleValueNtf : IAttPdu, IDecodable<AttHandleValueNtf>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_HANDLE_VALUE_NTF;
    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
    /// <summary> The handle of the attribute </summary>
    public required ushort Handle { get; init; }
    /// <summary> The current value of the attribute </summary>
    public required byte[] Value { get; init; }

    /// <inheritdoc />
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
            Value = span[3..].ToArray(),
        };
        return true;
    }
}