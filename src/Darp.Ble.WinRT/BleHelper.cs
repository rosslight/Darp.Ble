using Windows.Devices.Bluetooth;
using Darp.Ble.Data;

namespace Darp.Ble.WinRT;

public static class BleHelper
{
    public static BleAddress GetBleAddress(ulong winAddress, BluetoothAddressType winAddressType)
    {
        BleAddressType addressType = winAddressType switch
        {
            BluetoothAddressType.Public => BleAddressType.Public,
            BluetoothAddressType.Random => BleAddressType.RandomPrivateNonResolvable,
            _ => BleAddressType.NotAvailable
        };
        return new BleAddress(addressType, (UInt48)winAddress);
    }
}