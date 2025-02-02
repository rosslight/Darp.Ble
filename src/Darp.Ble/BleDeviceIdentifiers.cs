namespace Darp.Ble;

/// <summary> Well known <see cref="IBleDevice"/> identifiers </summary>
public static class BleDeviceIdentifiers
{
    /// <summary> Identifier of an android device </summary>
    public static readonly string Android = "Darp.Ble.Android";

    /// <summary> Identifier of a HCI host device </summary>
    public static readonly string HciHost = "Darp.Ble.HciHost";

    /// <summary> Identifier of a mock device </summary>
    public static readonly string Mock = "Darp.Ble.Mock";

    /// <summary> Identifier of a mocked device discoverable when using a mock </summary>
    public static readonly string MockDevice = "Darp.Ble.Mock.Device";

    /// <summary> Identifier of a Windows device </summary>
    public static readonly string WinRT = "Darp.Ble.WinRT";
}
