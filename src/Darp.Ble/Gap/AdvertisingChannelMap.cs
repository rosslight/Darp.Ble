namespace Darp.Ble.Gap;

/// <summary>Advertising channels used on the primary advertising PHY.</summary>
[Flags]
public enum AdvertisingChannelMap : byte
{
    /// <summary> The advertising channel 37 </summary>
    Channel37 = 1 << 0,

    /// <summary> The advertising channel 38 </summary>
    Channel38 = 1 << 1,

    /// <summary> The advertising channel 39 </summary>
    Channel39 = 1 << 2,
}
