using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary>
/// Example:
/// 010210002000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFE01008004000400000040000000000000000000000000000000000000
///         2000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFE01008004000400000040000000000000000000000000000000000000
/// Should be:
/// 0  - 7 : 00100000_00000000_10000000_00000000_00000000_11000000_00000000_00000000
/// 8  - 15: 00000000_00000000_11100100_00000000_00000000_00000000_00101000_00100010
/// 16 - 23 : 00000000_00000000_00000000_00000000_00000000_00000000_00000100_00000000
/// 24 - 31 : 00000000_11110111_11111111_11111111_01111111_00000000_00000000_00000000
/// 32 - 39 : 00110000_11000000_01111111_11111110_00000001_00000000_10000000_00000100
/// 40 - 47 : 00000000_00000100_00000000_00000000_00000000_01000000_00000000_00000000
/// 48 - 55 : 00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
/// 56 - 63 : 00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
///
/// In fields:
/// BitField0: 00000000_00000000_11000000_00000000_00000000_10000000_00000000_00100000
/// BitField1: 00100010_00101000_00000000_00000000_00000000_11100100_00000000_00000000
/// ...
/// Response to <see cref="HciReadLocalSupportedCommandsCommand"/>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciReadLocalSupportedCommandsResult : IDefaultDecodable<HciReadLocalSupportedCommandsResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    private readonly InlineBitField64 _bits;
    //public byte[] SupportedCommandBytes => ((ReadOnlySpan<byte>)_bits).ToArray();

    /// <summary> Get all supported commands </summary>
    public IEnumerable<HciOpCode> SupportedCommands => Enum
        .GetValues<HciOpCode>()
        .Where(IsSupported);

    /// <summary> Check if a command in a given octet and bit is supported </summary>
    /// <param name="octet"> The octet to look at </param>
    /// <param name="bit"> The bit to look at </param>
    /// <returns> True, if supported </returns>
    public bool IsSupported(byte octet, byte bit) => true;//(_bits[octet] & (1 << bit)) is not 0;

    /// <summary> Check if specific command is supported </summary>
    /// <param name="command"> The <see cref="HciOpCode"/> to check for </param>
    /// <returns> True, if supported </returns>
    public bool IsSupported(HciOpCode command) => command switch
    {
        HciOpCode.HCI_Disconnect => IsSupported(0, 5),
        HciOpCode.HCI_Read_Local_Supported_Commands => true,
        HciOpCode.HCI_Set_Event_Mask => IsSupported(5, 6),
        HciOpCode.HCI_Reset => IsSupported(5, 7),
        HciOpCode.HCI_Read_Local_Version_Information => IsSupported(14, 3),
        HciOpCode.HCI_LE_Set_Event_Mask => IsSupported(25, 0),
        HciOpCode.HCI_LE_Read_Buffer_Size_V1 => IsSupported(25, 1),
        HciOpCode.HCI_LE_Read_Local_Supported_Features => IsSupported(25, 2),
        HciOpCode.HCI_LE_Set_Random_Address => IsSupported(25, 4),
        HciOpCode.HCI_LE_Set_Data_Length => IsSupported(33, 6),
        HciOpCode.HCI_LE_Read_Suggested_Default_Data_Length => IsSupported(33, 7),
        HciOpCode.HCI_LE_Write_Suggested_Default_Data_Length => IsSupported(34, 0),
        HciOpCode.HCI_LE_Set_Extended_Scan_Parameters => IsSupported(37, 5),
        HciOpCode.HCI_LE_Set_Extended_Scan_Enable => IsSupported(37, 6),
        HciOpCode.HCI_LE_Extended_Create_ConnectionV1 => IsSupported(37, 7),
        HciOpCode.HCI_LE_Read_Buffer_Size_V2 => IsSupported(41, 5),
        HciOpCode.HCI_LE_Extended_Create_ConnectionV2 => IsSupported(47, 0),
        _ => false,
    };
}
