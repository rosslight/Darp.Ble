using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientDescriptor : GattClientDescriptor
{
    public WinGattClientDescriptor(
        WinGattClientCharacteristic clientCharacteristic,
        GattLocalDescriptor winDescriptor,
        IGattCharacteristicValue value
    )
        : base(clientCharacteristic, value) { }
}
