namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents error thrown by a <see cref="Device"/>
/// </summary>
public class BleDeviceException : Exception
{
    /// <summary> The BleDevice </summary>
    public IBleDevice Device { get; }

    /// <summary> Initializes the new exception </summary>
    /// <param name="device"> The ble device </param>
    /// <param name="message"> The message </param>
    public BleDeviceException(IBleDevice device, string? message) : base(message)
    {
        Device = device;
    }
}