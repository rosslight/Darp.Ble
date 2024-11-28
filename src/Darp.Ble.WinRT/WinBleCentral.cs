using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.WinRT.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

/// <inheritdoc />
internal sealed class WinBleCentral(BleDevice device, ILogger? logger) : BleCentral(device, logger)
{
    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<GattServerPeer>(async (observer, cancellationToken) =>
        {
            BluetoothLEDevice? winDev = await BluetoothLEDevice.FromBluetoothAddressAsync(address.Value, address.Type switch
            {
                BleAddressType.Public => BluetoothAddressType.Public,
                BleAddressType.NotAvailable => BluetoothAddressType.Unspecified,
                _ => BluetoothAddressType.Random,
            }).AsTask(cancellationToken);
            if (winDev is null)
            {
                observer.OnError(new Exception("PeripheralConnection: Failed!"));
                return;
            }
            observer.OnNext(new WinGattServerPeer(this, winDev, Logger));
        });
    }
}