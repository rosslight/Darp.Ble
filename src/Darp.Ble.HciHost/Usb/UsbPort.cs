using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Darp.Ble.HciHost.Usb;

/// <summary> Helpers when working with USB ports </summary>
public static class UsbPort
{
    /// <summary>
    /// Enumerates serial USB ports
    /// </summary>
    /// <returns>An enumerable with information about connected usb ports</returns>
    /// <exception cref="NotSupportedException">This code was executed on an operating system which is not supported</exception>
    [RequiresDynamicCode("Some dependencies might require dynamic code")]
    public static IEnumerable<UsbPortInfo> GetPortInfos()
    {
        if (OperatingSystem.IsWindows())
            return UsbPortWin.GetPortInfos();
        if (OperatingSystem.IsLinux())
            return UsbPortLinux.GetPortInfos();
        throw new NotSupportedException($"Invalid operating system {RuntimeInformation.OSDescription}");
    }

    /// <summary>
    /// Returns whether the serial port is already open
    /// </summary>
    /// <param name="portName">Port name (e.g. COM5)</param>
    /// <returns>The state of the port (true == open)</returns>
    /// <exception cref="NotSupportedException">This code was executed on an operating system which is not supported</exception>
    public static bool IsOpen(string portName)
    {
        if (OperatingSystem.IsWindows())
            return UsbPortWin.IsOpen(portName);
        if (OperatingSystem.IsLinux())
            return UsbPortLinux.IsOpen(portName);
        throw new NotSupportedException($"Invalid operating system {RuntimeInformation.OSDescription}");
    }
}
