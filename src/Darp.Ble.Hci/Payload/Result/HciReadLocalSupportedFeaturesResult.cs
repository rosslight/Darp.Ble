using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> The result of <see cref="HciReadLocalSupportedFeaturesCommand"/> </summary>
/// <param name="Status"> The command status </param>
/// <param name="LmpFeatures"> Bit Mask List of LMP features </param>
[BinaryObject]
public readonly partial record struct HciReadLocalSupportedFeaturesResult(HciCommandStatus Status, ulong LmpFeatures)
    : ICommandStatusResult;
