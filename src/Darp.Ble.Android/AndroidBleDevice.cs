using System.Diagnostics.CodeAnalysis;
using Android.Bluetooth;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public class AndroidBleDevice : IBleDeviceImplementation
{
    private readonly BluetoothManager _bluetoothManager;
    private BluetoothAdapter? BluetoothAdapter => _bluetoothManager.Adapter;

    public AndroidBleDevice(BluetoothManager bluetoothManager)
    {
        _bluetoothManager = bluetoothManager;
    }

    [MemberNotNullWhen(true, nameof(BluetoothAdapter))]
    public bool IsAvailable => BluetoothAdapter is not null;

    public Task<InitializeResult> InitializeAsync()
    {
        if (!IsAvailable) return Task.FromResult(InitializeResult.AdapterNotAvailable);
        if (BluetoothAdapter.BluetoothLeScanner is not null)
            Observer = new AndroidBleObserver(BluetoothAdapter.BluetoothLeScanner);
        return Task.FromResult(InitializeResult.Success);
    }

    public IBleObserverImplementation? Observer { get; private set; }
    public string Identifier => "Darp.Ble.Android";
}