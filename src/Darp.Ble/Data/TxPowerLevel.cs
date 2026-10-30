using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The Tx Power level </summary>
[SuppressMessage(
    "Design",
    "CA1008:Enums should have zero value",
    Justification = "Any value is a level in dBm; only the one for not available has a name"
)]
public enum TxPowerLevel : sbyte
{
    /// <summary> Not available / No preference </summary>
    NotAvailable = 0x7F,
}
