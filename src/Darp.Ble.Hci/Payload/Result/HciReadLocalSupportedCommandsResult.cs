using System.Buffers.Binary;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

// Example:
// 010210002000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFE01008004000400000040000000000000000000000000000000000000
//         2000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFE01008004000400000040000000000000000000000000000000000000
// Should be:
// 0  - 7 : 00100000_00000000_10000000_00000000_00000000_11000000_00000000_00000000
// 8  - 15: 00000000_00000000_11100100_00000000_00000000_00000000_00101000_00100010
// 16 - 23 : 00000000_00000000_00000000_00000000_00000000_00000000_00000100_00000000
// 24 - 31 : 00000000_11110111_11111111_11111111_01111111_00000000_00000000_00000000
// 32 - 39 : 00110000_11000000_01111111_11111110_00000001_00000000_10000000_00000100
// 40 - 47 : 00000000_00000100_00000000_00000000_00000000_01000000_00000000_00000000
// 48 - 55 : 00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
// 56 - 63 : 00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
//
// In fields:
// BitField0: 00000000_00000000_11000000_00000000_00000000_10000000_00000000_00100000
// BitField1: 00100010_00101000_00000000_00000000_00000000_11100100_00000000_00000000
// ...
// Response to <see cref="HciReadLocalSupportedCommandsCommand"/>
/// <summary> The result of <see cref="HciReadLocalSupportedCommandsCommand"/> </summary>
/// <param name="Status"> The command status </param>
/// <param name="Bits0"> Supported Command Bits </param>
/// <param name="Bits1"> Supported Command Bits </param>
/// <param name="Bits2"> Supported Command Bits </param>
/// <param name="Bits3"> Supported Command Bits </param>
/// <param name="Bits4"> Supported Command Bits </param>
/// <param name="Bits5"> Supported Command Bits </param>
/// <param name="Bits6"> Supported Command Bits </param>
/// <param name="Bits7"> Supported Command Bits </param>
[BinaryObject]
public readonly partial record struct HciReadLocalSupportedCommandsResult(HciCommandStatus Status,
    ulong Bits0, ulong Bits1, ulong Bits2, ulong Bits3, ulong Bits4, ulong Bits5, ulong Bits6, ulong Bits7)
{
    /// <summary> The supported commands </summary>
    /// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-d5f3af07-8495-3fe6-8afe-c6e6db371233"/> </remarks>
    [BinaryIgnore]
    private readonly InlineBitField64 _bits = GetBitField(Bits0, Bits1, Bits2, Bits3, Bits4, Bits5, Bits6, Bits7);

    private static InlineBitField64 GetBitField(ulong bits0, ulong bits1, ulong bits2, ulong bits3, ulong bits4,
        ulong bits5, ulong bits6, ulong bits7)
    {
        var bitfield = default(InlineBitField64);
        Span<byte> bitfieldSpan = bitfield;
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan, bits0);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[8..], bits1);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[16..], bits2);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[24..], bits3);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[32..], bits4);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[40..], bits5);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[48..], bits6);
        BinaryPrimitives.WriteUInt64LittleEndian(bitfieldSpan[56..], bits7);
        return bitfield;
    }
    //public byte[] SupportedCommandBytes => ((ReadOnlySpan<byte>)_bits).ToArray();

    /// <summary> Get all supported commands </summary>
    public IEnumerable<HciOpCode> SupportedCommands => Enum
        .GetValues<HciOpCode>()
        .Where(IsSupported);

    /// <summary> Check if a command in a given octet and bit is supported </summary>
    /// <param name="octet"> The octet to look at </param>
    /// <param name="bit"> The bit to look at </param>
    /// <returns> True, if supported </returns>
    public bool IsSupported(byte octet, byte bit) => (_bits[octet] & (1 << bit)) is not 0;

    /// <summary> Check if specific command is supported </summary>
    /// <param name="command"> The <see cref="HciOpCode"/> to check for </param>
    /// <returns> True, if supported </returns>
    public bool IsSupported(HciOpCode command) => command switch
    {
        HciOpCode.None => false,
        HciOpCode.HCI_Disconnect => IsSupported(0, 5),
        HciOpCode.HCI_Read_Local_Supported_Commands => true,
        HciOpCode.HCI_Set_Event_Mask => IsSupported(5, 6),
        HciOpCode.HCI_Reset => IsSupported(5, 7),
        HciOpCode.HCI_Read_Local_Version_Information => IsSupported(14, 3),
        HciOpCode.HCI_Read_BD_ADDR => IsSupported(15, 1),
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
