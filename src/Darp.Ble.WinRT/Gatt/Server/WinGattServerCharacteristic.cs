using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattServerCharacteristic(GattCharacteristic gattCharacteristic)
    : GattServerCharacteristic(new BleUuid(gattCharacteristic.Uuid, inferType: true))
{
    private readonly GattCharacteristic _gattCharacteristic = gattCharacteristic;

    protected override async Task WriteInternalAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await _gattCharacteristic.WriteValueAsync(bytes.AsBuffer()).AsTask(cancellationToken);
    }
}