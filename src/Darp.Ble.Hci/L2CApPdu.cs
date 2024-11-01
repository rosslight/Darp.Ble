namespace Darp.Ble.Hci;

public readonly struct L2CApPdu
{
    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required uint ConnectionHandle { get; init; }
    public required byte[] Pdu { get; init; }
}