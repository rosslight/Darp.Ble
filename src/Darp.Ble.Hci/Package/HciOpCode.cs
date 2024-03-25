using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Package;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HciOpCode : ushort
{
    // Link Control
    HCI_Disconnect = 0x0006 | (HciOpCodeGroupField.LinkControl << 10),

    // Controller
    HCI_Set_Event_Mask = 0x0001 | (HciOpCodeGroupField.Controller << 10),
    HCI_Reset = 0x0003 | (HciOpCodeGroupField.Controller << 10),

    // Informational
    HCI_Read_Local_Version_Information = 0x0001 | (HciOpCodeGroupField.Informational << 10),
    HCI_Read_Local_Supported_Commands = 0x0002 | (HciOpCodeGroupField.Informational << 10),
    HCI_Read_BD_ADDR = 0x0009 | (HciOpCodeGroupField.Informational << 10),

    // LeController
    HCI_LE_Set_Event_Mask = 0x0001 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Read_Buffer_Size_V1 = 0x0002 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Read_Local_Supported_Features = 0x0003 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Set_Random_Address = 0x0005 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Set_Data_Length = 0x0022 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Read_Suggested_Default_Data_Length = 0x0023 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Write_Suggested_Default_Data_Length = 0x0024| (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Set_Extended_Scan_Parameters = 0x0041 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Set_Extended_Scan_Enable = 0x0042 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Extended_Create_ConnectionV1 = 0x0043 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Read_Buffer_Size_V2 = 0x0060 | (HciOpCodeGroupField.LeController << 10),
    HCI_LE_Extended_Create_ConnectionV2 = 0x0085 | (HciOpCodeGroupField.LeController << 10),
}