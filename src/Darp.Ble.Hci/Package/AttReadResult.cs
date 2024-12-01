using Darp.Ble.Hci.Payload.Att;

namespace Darp.Ble.Hci.Package;

/// <summary> The read result of an att command </summary>
/// <param name="OpCode"> The OpCode </param>
/// <param name="Pdu"> The PDU </param>
public readonly record struct AttReadResult(AttOpCode OpCode, byte[] Pdu);
