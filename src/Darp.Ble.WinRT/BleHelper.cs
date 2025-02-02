using Darp.Ble.Data;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

public static class BleHelper
{
    public static BleAddress GetBleAddress(ulong winAddress, BluetoothAddressType winAddressType)
    {
        BleAddressType addressType = winAddressType switch
        {
            BluetoothAddressType.Public => BleAddressType.Public,
            BluetoothAddressType.Random => BleAddressType.RandomPrivateNonResolvable,
            _ => BleAddressType.NotAvailable,
        };
        return new BleAddress(addressType, (UInt48)winAddress);
    }
}
