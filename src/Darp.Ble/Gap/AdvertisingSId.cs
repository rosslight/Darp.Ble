using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Gap;

/// <summary> The advertising SId </summary>
/// <remarks> Range: 0x00 to 0x0F </remarks>
[SuppressMessage(
    "Design",
    "CA1008:Enums should have zero value",
    Justification = "Any value from 0x00 to 0x0F is a set id; only the one for not provided has a name"
)]
public enum AdvertisingSId : byte
{
    /// <summary> No ADI field provide </summary>
    NoAdIProvided = 0xFF,
}
