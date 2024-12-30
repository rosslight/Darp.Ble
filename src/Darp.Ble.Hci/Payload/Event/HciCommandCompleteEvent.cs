using System.Buffers.Binary;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Command_Complete event is used by the Controller for most commands to transmit return status of a command and the other event parameters that are specified for the issued HCI command </summary>
/// <typeparam name="TParameters"></typeparam>
public readonly record struct HciCommandCompleteEvent<TParameters> : IHciEvent<HciCommandCompleteEvent<TParameters>>
    where TParameters : IBinaryReadable<TParameters>
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
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciCommandCompleteEvent<TParameters> value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciCommandCompleteEvent<TParameters> value, out int bytesRead)
    {
        bytesRead = 0;
        value = default;
        if (source.Length < 3)
            return false;
        byte numHciCommandPackets = source[0];
        ushort commandOpCode = BinaryPrimitives.ReadUInt16LittleEndian(source[1..]);
        if (!TParameters.TryReadLittleEndian(source[3..], out TParameters? returnParameters, out int parameterBytesRead))
            return false;
        bytesRead = 3 + parameterBytesRead;
        value = new HciCommandCompleteEvent<TParameters>
        {
            NumHciCommandPackets = numHciCommandPackets,
            CommandOpCode = (HciOpCode)commandOpCode,
            ReturnParameters = returnParameters,
        };
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciCommandCompleteEvent<TParameters> value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciCommandCompleteEvent<TParameters> value, out int bytesRead)
    {
        throw new NotSupportedException();
    }
}