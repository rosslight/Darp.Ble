namespace Darp.Ble.Hci;

public readonly struct L2CApPdu
{
    public required uint ConnectionHandle { get; init; }
    public required byte[] Pdu { get; init; }
}