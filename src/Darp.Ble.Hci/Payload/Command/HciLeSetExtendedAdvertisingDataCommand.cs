using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Advertising_Data command is used to set the data used in advertising PDUs that have a data field
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetExtendedAdvertisingDataResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-d4f36cb5-f26c-d053-1034-e7a547ed6a13"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedAdvertisingDataCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_DATA;
    /// <summary> Advertising_Handle Used to identify an advertising set </summary>
    /// <value> 0x00 to 0xEF </value>
    public required byte AdvertisingHandle { get; init; }
    /// <summary> Operation </summary>
    public required byte Operation { get; init; }
    /// <summary> Fragment_Preference </summary>
    public required byte FragmentPreference { get; init; }
    /// <summary> Advertising_Data_Length </summary>
    public required byte AdvertisingDataLength { get; init; }
    /// <summary> Advertising_Data </summary>
    [BinaryElementCount(nameof(AdvertisingDataLength))]
    public required ReadOnlyMemory<byte> AdvertisingData { get; init; }
}