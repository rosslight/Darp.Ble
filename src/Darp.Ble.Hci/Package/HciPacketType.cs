namespace Darp.Ble.Hci.Package;

/// <summary> The type of the HCI Packets </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-669ddd4e-ba72-9009-6f07-2f71de9c9a7e"/>
public enum HciPacketType : byte
{
    /// <summary> Invalid packet type </summary>
    None,
    /// <summary> The <see cref="HciCommandPacket{TParameters}"/> </summary>
    HciCommand = 0x01,
    /// <summary> The <see cref="HciAclPacket{TData}"/> </summary>
    HciAclData = 0x02,
    /// <summary> The Hci Synchronous data packet </summary>
    HciSynchronousData = 0x03,
    /// <summary> The <see cref="HciEventPacket"/> </summary>
    HciEvent = 0x04,
    /// <summary> The Hci Iso Data Packet </summary>
    HciIsoData = 0x05,
}
