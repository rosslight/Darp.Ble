using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The ble uuid type </summary>
[SuppressMessage(
    "Design",
    "CA1008:Enums should have zero value",
    Justification = "The values are the lengths in bytes; every UUID has one"
)]
[SuppressMessage(
    "Design",
    "CA1027:Mark enums with FlagsAttribute",
    Justification = "The values are lengths, not flags"
)]
public enum BleUuidType
{
    /// <summary> Ble Uuid with 16 bits </summary>
    Uuid16 = 2,

    /// <summary> Ble Uuid with 32 bits </summary>
    Uuid32 = 4,

    /// <summary> Ble Uuid with 128 bits </summary>
    Uuid128 = 16,
}
