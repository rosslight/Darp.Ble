namespace Darp.Ble.Hci.Payload.Att;

/// <summary> A att pdu </summary>
public interface IAttPdu
{
    /// <summary> The static OpCode of the att pdu </summary>
    static abstract AttOpCode ExpectedOpCode { get; }
    /// <summary> The OpCode of the att pdu </summary>
    AttOpCode OpCode { get; }
}