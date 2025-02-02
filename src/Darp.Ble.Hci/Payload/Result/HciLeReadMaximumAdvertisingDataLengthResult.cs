using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadMaximumAdvertisingDataLengthCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeReadMaximumAdvertisingDataLengthResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> Max_Advertising_Data_Length </summary>
    /// <value> 0x001F to 0x0672 </value>
    public required ushort MaxAdvertisingDataLength { get; init; }
}
