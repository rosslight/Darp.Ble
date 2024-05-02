using Darp.Ble.Hci.Payload.Att;

namespace Darp.Ble.Hci.Package;

public readonly record struct AttReadResult(AttOpCode OpCode, byte[] Pdu);