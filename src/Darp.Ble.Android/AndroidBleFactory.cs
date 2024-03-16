using Android.Bluetooth;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public sealed class AndroidBleFactory : IBleFactory
{
    private readonly BluetoothManager _bluetoothManager;

    public AndroidBleFactory(BluetoothManager manager)
    {
        _bluetoothManager = manager;
    }

    public IEnumerable<IBleDeviceImplementation> EnumerateAdapters()
    {
        if (_bluetoothManager.Adapter is null) yield break;
        yield return new AndroidBleDevice(_bluetoothManager);
    }
}