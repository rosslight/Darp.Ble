namespace Darp.Ble.Hci.Package;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 5.4.2 HCI ACL Data packets </summary>
public enum PacketBoundaryFlag
{
    /// <summary>
    /// First non-automatically-flushable packet of a higher layer message
    /// (start of a non-automatically-flushable L2CAP PDU) from Host to Controller.
    /// </summary>
    FirstNonAutoFlushable = 0b00,
    /// <summary>
    /// Continuing fragment of a higher layer message
    /// </summary>
    ContinuingFragment = 0b01,
    /// <summary>
    /// First automatically flushable packet of a higher layer message (start of an automatically-flushable L2CAP PDU)
    /// </summary>
    FirstAutoFlushable = 0b10,
}