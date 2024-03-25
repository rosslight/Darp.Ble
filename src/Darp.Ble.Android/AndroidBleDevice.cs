using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android;
using Android.Bluetooth;
using Android.Content.PM;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public sealed class AndroidBleDevice(BluetoothManager bluetoothManager) : IPlatformSpecificBleDevice
{
    private readonly BluetoothManager _bluetoothManager = bluetoothManager;
    private BluetoothAdapter? BluetoothAdapter => _bluetoothManager.Adapter;

    [MemberNotNullWhen(true, nameof(BluetoothAdapter))]
    public bool IsAvailable => BluetoothAdapter?.IsEnabled == true;

    [SupportedOSPlatform("android31.0")]
    public Task<InitializeResult> InitializeAsync()
    {
        if (BluetoothAdapter is null) return Task.FromResult(InitializeResult.DeviceNotAvailable);
        if (BluetoothAdapter.IsEnabled) return Task.FromResult(InitializeResult.DeviceNotEnabled);
        if (!IsAvailable) return Task.FromResult(InitializeResult.DeviceNotAvailable);

        if (Application.Context.CheckSelfPermission(Manifest.Permission.BluetoothScan) is Permission.Granted
            && BluetoothAdapter.BluetoothLeScanner is not null)
        {
            Observer = new AndroidBleObserver(BluetoothAdapter.BluetoothLeScanner);
        }
        return Task.FromResult(InitializeResult.Success);
    }

    public IPlatformSpecificBleObserver? Observer { get; private set; }
    public object Central => throw new NotImplementedException();
    public string Identifier => "Darp.Ble.Android";

    void IDisposable.Dispose()
    {
        _bluetoothManager.Dispose();
        bluetoothManager.Dispose();
    }
}