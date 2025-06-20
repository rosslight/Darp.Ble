using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.WinRT.Gatt.Server;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

/// <inheritdoc />
internal sealed class WinBleCentral(BleDevice device, ILogger<WinBleCentral> logger) : BleCentral(device, logger)
{
    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(
        BleAddress address,
        BleConnectionParameters connectionParameters,
        BleObservationParameters observationParameters
    )
    {
        return Observable.Create<GattServerPeer>(
            async (observer, cancellationToken) =>
            {
                BluetoothLEDevice? winDev = await BluetoothLEDevice
                    .FromBluetoothAddressAsync(
                        address.Value,
                        address.Type switch
                        {
                            BleAddressType.Public => BluetoothAddressType.Public,
                            BleAddressType.NotAvailable => BluetoothAddressType.Unspecified,
                            _ => BluetoothAddressType.Random,
                        }
                    )
                    .AsTask(cancellationToken)
                    .ConfigureAwait(false);
                if (winDev is null)
                {
                    observer.OnError(new Exception("PeripheralConnection: Failed!"));
                    return;
                }
                observer.OnNext(new WinGattServerPeer(this, winDev, ServiceProvider.GetLogger<WinGattServerPeer>()));
            }
        );
    }
}
