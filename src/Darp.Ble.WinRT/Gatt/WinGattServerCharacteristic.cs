using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattServerCharacteristic(GattCharacteristic gattCharacteristic)
    : IPlatformSpecificGattServerCharacteristic
{
    private readonly GattCharacteristic _gattCharacteristic = gattCharacteristic;
}