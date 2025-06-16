using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Advertising_Enable command is used to request the Controller to enable or disable one or more advertising sets using the advertising sets identified by the Advertising_Handle[i] parameter.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetExtendedAdvertisingEnableResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e521f1e8-a463-d700-c289-408939099a5d"/> </remarks>
[BinaryObject]
[StructLayout(LayoutKind.Auto)]
public readonly partial record struct HciLeSetExtendedAdvertisingEnableCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_ENABLE;

    /// <summary> Enable </summary>
    public required byte Enable { get; init; }

    /// <summary> Num_Sets </summary>
    public required byte NumSets { get; init; }

    /// <summary> Advertising_Handle </summary>
    [BinaryElementCount(nameof(NumSets))]
    public required ReadOnlyMemory<byte> AdvertisingHandle { get; init; }

    /// <summary> Duration </summary>
    [BinaryElementCount(nameof(NumSets))]
    public required ReadOnlyMemory<ushort> Duration { get; init; }

    /// <summary> Max_Extended_Advertising_Events </summary>
    [BinaryElementCount(nameof(NumSets))]
    public required ReadOnlyMemory<byte> MaxExtendedAdvertisingEvents { get; init; }
}
