using System.Reactive.Disposables;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientService(WinBlePeripheral peripheral, GattServiceProvider provider) : GattClientService(new BleUuid(provider.Service.Uuid, true))
{
    private readonly GattServiceProvider _serviceProvider = provider;
    private readonly GattLocalService _winService = provider.Service;
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

    public IDisposable Advertise(AdvertisingParameters? parameters)
    {
        var winParameters = new GattServiceProviderAdvertisingParameters();
        if (parameters?.Type.HasFlag(BleEventType.Connectable) is true)
            winParameters.IsConnectable = true;
        winParameters.IsDiscoverable = true;
        _serviceProvider.StartAdvertising(winParameters);
        return Disposable.Create(_serviceProvider, provider => provider.StopAdvertising());
    }
}