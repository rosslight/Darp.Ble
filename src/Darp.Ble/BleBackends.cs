namespace Darp.Ble;

/// <summary> The names of the backends, as shown in <see cref="BleAdapterInfo.Backend"/> </summary>
public static class BleBackends
{
    /// <summary> The in-process mock backend from the Darp.Ble.Mock package </summary>
    public const string Mock = "Darp.Ble.Mock";

    /// <summary> The HCI backend from the Darp.Ble.HciHost package, which drives a controller over a serial port </summary>
    public const string HciHost = "Darp.Ble.HciHost";

    /// <summary> The Windows backend from the Darp.Ble.WinRT package, on Windows' own Bluetooth stack </summary>
    public const string WinRT = "Darp.Ble.WinRT";

    /// <summary> The Android backend from the Darp.Ble.Android package, on Android's own Bluetooth stack </summary>
    public const string Android = "Darp.Ble.Android";
}
