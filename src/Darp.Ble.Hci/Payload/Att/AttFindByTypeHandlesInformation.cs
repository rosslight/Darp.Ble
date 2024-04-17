namespace Darp.Ble.Hci.Payload.Att;

public readonly record struct AttFindByTypeHandlesInformation(ushort Handle, ushort EndGroup);