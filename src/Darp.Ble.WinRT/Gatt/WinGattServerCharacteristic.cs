using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattServerCharacteristic(GattCharacteristic gattCharacteristic)
    : IPlatformSpecificGattServerCharacteristic
{
    private readonly GattCharacteristic _gattCharacteristic = gattCharacteristic;
    public BleUuid Uuid { get; } = new (gattCharacteristic.Uuid, inferType: true);

    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await _gattCharacteristic.WriteValueAsync(bytes.AsBuffer()).AsTask(cancellationToken);
    }
}