namespace Darp.Ble.Hci.Package;

public enum HciOpCodeGroupField : byte
{
    /// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 7.1 LINK CONTROL COMMANDS </summary>
    LinkControl = 0x01,
    Controller = 0x03,
    Informational = 0x04,
    LeController = 0x08,
}