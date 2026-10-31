using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> A server indicates an attribute's value; the client confirms it before the server indicates again </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-40393db4-55e7-1a22-5eff-2bbcce21de5d"/>
[BinaryObject]
public readonly partial record struct AttHandleValueInd() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_HANDLE_VALUE_IND;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> The handle of the attribute </summary>
    public required ushort Handle { get; init; }

    /// <summary> The current value of the attribute </summary>
    public required ReadOnlyMemory<byte> Value { get; init; }
}
