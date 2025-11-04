using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary>
/// Response to <see cref="HciLeReadPhyCommand"/> (HCI_LE_Read_PHY).
/// OpCode: <see cref="HciOpCode.HCI_LE_READ_PHY"/>.
/// </summary>
/// <remarks>https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bf5bec23-cd60-0648-9342-fa6d0886de40</remarks>
[BinaryObject]
public readonly partial record struct HciLeReadPhyResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }

    /// <summary> Connection_Handle (Range: 0x0000 to 0x0EFF) </summary>
    public required ushort ConnectionHandle { get; init; }

    /// <summary>
    /// TX_PHY: The transmitter PHY for the connection
    /// 0x01 = LE 1M, 0x02 = LE 2M, 0x03 = LE Coded, other values reserved
    /// </summary>
    public required byte TxPhy { get; init; }

    /// <summary>
    /// RX_PHY: The receiver PHY for the connection
    /// 0x01 = LE 1M, 0x02 = LE 2M, 0x03 = LE Coded, other values reserved
    /// </summary>
    public required byte RxPhy { get; init; }
}
