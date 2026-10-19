using Darp.Ble.Data;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

internal static class BleHelper
{
    public static BleAddress GetBleAddress(ulong winAddress, BluetoothAddressType winAddressType)
    {
        BleAddressType addressType = winAddressType switch
        {
            BluetoothAddressType.Public => BleAddressType.Public,
            BluetoothAddressType.Random => BleAddressType.Random,
            _ => BleAddressType.NotAvailable,
        };
        return new BleAddress(addressType, (UInt48)winAddress);
    }
}
