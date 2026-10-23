namespace Darp.Ble.Gatt.Services;

/// <summary> Where the heart rate sensor sits </summary>
public enum HeartRateBodySensorLocation : byte
{
    /// <summary> Another location </summary>
    Other = 0,

    /// <summary> Chest </summary>
    Chest = 1,

    /// <summary> Wrist </summary>
    Wrist = 2,

    /// <summary> Finger </summary>
    Finger = 3,

    /// <summary> Hand </summary>
    Hand = 4,

    /// <summary> Ear lobe </summary>
    EarLobe = 5,

    /// <summary> Foot </summary>
    Foot = 6,
}
