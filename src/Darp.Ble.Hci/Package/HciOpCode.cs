using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Package;

/// <summary> The Supported_Commands configuration parameter lists which HCI commands the local Controller supports </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-d5f3af07-8495-3fe6-8afe-c6e6db371233"/>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public enum HciOpCode : ushort
{
    /// <summary> Invalid Hci command </summary>
    None,

    // Link Control
    /// <summary> The <see cref="HciDisconnectCommand"/> </summary>
    HCI_Disconnect = 0x0006 | (HciOpCodeGroupField.LinkControl << 10),

    // Controller
    /// <summary> <see cref="HciSetEventMaskCommand"/> </summary>
    HCI_Set_Event_Mask = 0x0001 | (HciOpCodeGroupField.Controller << 10),
    /// <summary> The <see cref="HciResetCommand"/> </summary>
    HCI_Reset = 0x0003 | (HciOpCodeGroupField.Controller << 10),

    // Informational
    /// <summary> The <see cref="HciReadLocalVersionInformationCommand"/> </summary>
    HCI_Read_Local_Version_Information = 0x0001 | (HciOpCodeGroupField.Informational << 10),
    /// <summary> The <see cref="HciReadLocalSupportedCommandsCommand"/> </summary>
    HCI_Read_Local_Supported_Commands = 0x0002 | (HciOpCodeGroupField.Informational << 10),
    /// <summary> The <see cref="HciReadBdAddrCommand"/> </summary>
    HCI_Read_BD_ADDR = 0x0009 | (HciOpCodeGroupField.Informational << 10),

    // LeController
    /// <summary> The <see cref="HciLeSetEventMaskCommand"/> </summary>
    HCI_LE_Set_Event_Mask = 0x0001 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeReadBufferSizeCommandV1"/> </summary>
    HCI_LE_Read_Buffer_Size_V1 = 0x0002 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeReadLocalSupportedFeaturesCommand"/> </summary>
    HCI_LE_Read_Local_Supported_Features = 0x0003 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeSetRandomAddressCommand"/> </summary>
    HCI_LE_Set_Random_Address = 0x0005 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeSetDataLengthCommand"/> </summary>
    HCI_LE_Set_Data_Length = 0x0022 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeReadSuggestedDefaultDataLengthCommand"/> </summary>
    HCI_LE_Read_Suggested_Default_Data_Length = 0x0023 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeWriteSuggestedDefaultDataLengthCommand"/> </summary>
    HCI_LE_Write_Suggested_Default_Data_Length = 0x0024| (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeSetExtendedScanParametersCommand"/> </summary>
    HCI_LE_Set_Extended_Scan_Parameters = 0x0041 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeSetExtendedScanEnableCommand"/> </summary>
    HCI_LE_Set_Extended_Scan_Enable = 0x0042 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The <see cref="HciLeExtendedCreateConnectionV1Command"/> </summary>
    HCI_LE_Extended_Create_ConnectionV1 = 0x0043 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The read buffer size v2 command </summary>
    HCI_LE_Read_Buffer_Size_V2 = 0x0060 | (HciOpCodeGroupField.LeController << 10),
    /// <summary> The extended create connection v2 command </summary>
    HCI_LE_Extended_Create_ConnectionV2 = 0x0085 | (HciOpCodeGroupField.LeController << 10),
}