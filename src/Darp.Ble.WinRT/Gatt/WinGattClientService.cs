using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Utils;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientService(WinBlePeripheral peripheral, GattServiceProvider provider)
    : GattClientService(peripheral, BleUuid.FromGuid(provider.Service.Uuid, inferType: true))
{
    private readonly GattServiceProvider _serviceProvider = provider;
    private readonly GattLocalService _winService = provider.Service;
    public new WinBlePeripheral Peripheral { get; } = peripheral;

    protected override async Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid,
        GattProperty gattProperty,
        IGattClientService.OnReadCallback? onRead,
        IGattClientService.OnWriteCallback? onWrite,
        CancellationToken cancellationToken)
    {
        GattLocalCharacteristicResult result = await _winService.CreateCharacteristicAsync(uuid.Value,
            new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = (GattCharacteristicProperties)gattProperty,
            })
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nopiii");
        result.Characteristic.SubscribedClientsChanged += (sender, _) =>
        {
            foreach (GattSubscribedClient senderSubscribedClient in sender.SubscribedClients)
            {
                Peripheral.GetOrRegisterSession(senderSubscribedClient.Session);
            }
        };
        return new WinGattClientCharacteristic(this, result.Characteristic, onRead, onWrite);
    }

    public IAsyncDisposable Advertise(IAdvertisingSet advertisingSet)
    {
        AdvertisingParameters parameters = advertisingSet.Parameters;
        var winParameters = new GattServiceProviderAdvertisingParameters();
        if (parameters.Type.HasFlag(BleEventType.Connectable))
            winParameters.IsConnectable = true;
        winParameters.IsDiscoverable = true;
        if (advertisingSet.Data.TryGetFirstType(AdTypes.ServiceData16BitUuid, out ReadOnlyMemory<byte> memory))
        {
            winParameters.ServiceData = memory.ToArray().AsBuffer();
        }
        _serviceProvider.StartAdvertising(winParameters);
        return AsyncDisposable.Create(_serviceProvider, provider =>
        {
            provider.StopAdvertising();
            return ValueTask.CompletedTask;
        });
    }
}