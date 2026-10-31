using Windows.Devices.Radios;

namespace Darp.Ble.WinRT;

/// <summary>
/// Windows' Bluetooth radio. A scan or a service started while the radio is off waits silently until it is on, so the
/// backend refuses to start one then
/// </summary>
internal static class WinRTRadio
{
    public static void ThrowIfOff(Radio radio)
    {
        if (radio.State is not RadioState.On)
            throw new BleException(
                $"The Bluetooth radio is {radio.State}",
                isTransient: true,
                nativeCode: null,
                innerException: null
            );
    }
}
