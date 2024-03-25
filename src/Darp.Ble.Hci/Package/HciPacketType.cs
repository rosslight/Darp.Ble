namespace Darp.Ble.Hci.Package;

// Volume 4, Part E, 5.4
public enum HciPacketType : byte
{
    HciCommand = 0x01,
    HciAclData = 0x02,
//    HciSynchronousData = 0x03,
    HciEvent = 0x04,
    //    HciIsoData = 0x05
}