using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.WinRT.Gatt;

namespace Darp.Ble.WinRT;

/// <inheritdoc />
public sealed class WinBleCentral : IPlatformSpecificBleCentral
{
    /// <inheritdoc />
    public IObservable<GattServerDevice> ConnectToPeripheral(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<GattServerDevice>(async (observer, cancellationToken) =>
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
            var winGattDevice = new WinGattServerDevice(winDev);
            observer.OnNext(new GattServerDevice(winGattDevice, winDev.ConnectionStatus is BluetoothConnectionStatus.Connected));
        });
    }
}