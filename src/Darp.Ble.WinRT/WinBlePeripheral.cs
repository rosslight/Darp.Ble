using System.Reactive.Disposables;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Utils;
using Darp.Ble.WinRT.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

internal sealed class WinBlePeripheral(WinBleDevice device, ILogger? logger) : BlePeripheral(device, logger)
{
    protected override async Task<IGattClientService> AddServiceAsyncCore(BleUuid uuid,
        CancellationToken cancellationToken)
    {
        GattServiceProviderResult result = await GattServiceProvider
            .CreateAsync(uuid.Value)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nope");
        GattServiceProvider provider = result.ServiceProvider;
        return new WinGattClientService(this, provider);
    }

    internal IGattClientPeer GetOrRegisterSession(GattSession gattSession)
    {
        BleAddress address = BleAddress.Parse(gattSession.DeviceId.Id[^17..], provider: null);
        if (PeerDevices.TryGetValue(address, out IGattClientPeer? clientPeer) && clientPeer.IsConnected)
        {
            return clientPeer;
        }
        clientPeer = new WinGattClientPeer(gattSession, address);
        OnConnectedCentral(clientPeer);
        return clientPeer;
    }

    public IAsyncDisposable AdvertiseServices(IAdvertisingSet advertisingSet)
    {
        List<IAsyncDisposable> disposables = [];
        foreach ((BleUuid _, IGattClientService value) in Services)
        {
            if (value is WinGattClientService service)
                disposables.Add(service.Advertise(advertisingSet));
        }
        return AsyncDisposable.Create(disposables, async list =>
        {
            foreach (IAsyncDisposable disposable in list)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        });
    }
}