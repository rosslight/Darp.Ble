using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The rssi measured </summary>
[SuppressMessage(
    "Design",
    "CA1008:Enums should have zero value",
    Justification = "Any value is a level in dBm; only the one for not available has a name"
)]
public enum Rssi : sbyte
{
    /// <summary> Rssi is not available </summary>
    NotAvailable = 0x7F,
}
