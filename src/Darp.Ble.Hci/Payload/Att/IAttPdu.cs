namespace Darp.Ble.Hci.Payload.Att;

public interface IAttPdu
{
    static abstract AttOpCode ExpectedOpCode { get; }
    AttOpCode OpCode { get; }
}