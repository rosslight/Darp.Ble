namespace Darp.Ble.Hci.Payload.Att;

public readonly record struct AttInformationData(ushort Handle, ushort Uuid);