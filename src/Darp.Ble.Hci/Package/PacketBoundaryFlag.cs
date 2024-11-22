namespace Darp.Ble.Hci.Package;

/// <summary> The Packet_Boundary_Flag </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bc4ffa33-44ef-e93c-16c8-14aa99597cfc"/>
public enum PacketBoundaryFlag
{
    /// <summary>
    /// First non-automatically-flushable packet of a higher layer message
    /// (start of a non-automatically-flushable L2CAP PDU) from Host to Controller.
    /// </summary>
    FirstNonAutoFlushable = 0b00,
    /// <summary> Continuing fragment of a higher layer message </summary>
    ContinuingFragment = 0b01,
    /// <summary> First automatically flushable packet of a higher layer message (start of an automatically-flushable L2CAP PDU) </summary>
    FirstAutoFlushable = 0b10,
}