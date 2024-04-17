using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Thrown if the device was not initialized </summary>
/// <param name="device"> The ble device </param>
public sealed class NotInitializedException(BleDevice device) : BleDeviceException(device, "Could not execute action. Did you initialize the device?");