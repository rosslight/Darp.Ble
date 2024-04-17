using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattClientService(WinBlePeripheral peripheral, GattLocalService winService) : GattClientService(new BleUuid(winService.Uuid, true))
{
    private readonly GattLocalService _winService = winService;
    public WinBlePeripheral Peripheral { get; } = peripheral;

    protected override async Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken)
    {
        GattLocalCharacteristicResult result = await _winService.CreateCharacteristicAsync(uuid.Value,
            new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = (GattCharacteristicProperties)gattProperty,
            }).AsTask(cancellationToken);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nopiii");
        result.Characteristic.SubscribedClientsChanged += (sender, args) =>
        {
            foreach (GattSubscribedClient senderSubscribedClient in sender.SubscribedClients)
            {
                Peripheral.GetOrRegisterSession(senderSubscribedClient.Session);
            }
        };
        return new WinGattClientCharacteristic(this, result.Characteristic);
    }
}