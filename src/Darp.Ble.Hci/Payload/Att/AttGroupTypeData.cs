namespace Darp.Ble.Hci.Payload.Att;

public readonly record struct AttGroupTypeData<TAttributeValue>(ushort Handle, ushort EndGroup, TAttributeValue Value)
    where TAttributeValue : unmanaged;