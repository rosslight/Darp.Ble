using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadNumberOfSupportedAdvertisingSetsCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeReadNumberOfSupportedAdvertisingSetsResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> Number of advertising sets supported at the same time </summary>
    public required byte NumSupportedAdvertisingSets { get; init; }
}
