namespace Darp.Ble.Data;

/// <summary> Supplement to the Bluetooth Core Specification | v11, Part A </summary>
[Flags]
public enum AdvertisingDataFlags : byte
{
    /// <summary>Specifies no flag.</summary>
    None = 0b00000000,
    /// <summary>Specifies Bluetooth LE Limited Discoverable Mode.</summary>
    LimitedDiscoverableMode = 0b00000001,
    /// <summary>Specifies Bluetooth LE General Discoverable Mode.</summary>
    GeneralDiscoverableMode = 0b00000010,
    /// <summary>Specifies Bluetooth BR/EDR not supported.</summary>
    ClassicNotSupported = 0b00000100,
    /// <summary>Specifies simultaneous Bluetooth LE and BR/EDR to same device capable (controller).</summary>
    DualModeControllerCapable = 0b00001000,
    /// <summary>Specifies simultaneous Bluetooth LE and BR/EDR to same device capable (host)</summary>
    DualModeHostCapable = 0b00010000, // 0x00000010
}