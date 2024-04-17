namespace Darp.Ble.Hci.Payload.Att;

public readonly record struct AttTypeData(ushort Handle, byte[] Value);