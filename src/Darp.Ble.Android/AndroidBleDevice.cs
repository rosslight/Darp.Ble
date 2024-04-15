using System.Diagnostics.CodeAnalysis;
using Android;
using Android.Bluetooth;
using Android.Content.PM;
using Darp.Ble.Data;
using Darp.Ble.Logger;

namespace Darp.Ble.Android;

public sealed class AndroidBleDevice(BluetoothManager bluetoothManager, IObserver<(BleDevice, LogEvent)>? logger)
    : BleDevice(logger)
{
    private readonly BluetoothManager _bluetoothManager = bluetoothManager;
    private BluetoothAdapter? BluetoothAdapter => _bluetoothManager.Adapter;

    [MemberNotNullWhen(true, nameof(BluetoothAdapter))]
    public bool IsAvailable => BluetoothAdapter?.IsEnabled == true;

    public override string? Name => BluetoothAdapter?.Name;

    protected override Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        if (BluetoothAdapter is null) return Task.FromResult(InitializeResult.DeviceNotAvailable);
        if (!BluetoothAdapter.IsEnabled) return Task.FromResult(InitializeResult.DeviceNotEnabled);
        if (!IsAvailable) return Task.FromResult(InitializeResult.DeviceNotAvailable);

        if (HasScanPermissions()
            && BluetoothAdapter.BluetoothLeScanner is not null)
        {
            Observer = new AndroidBleObserver(this, BluetoothAdapter.BluetoothLeScanner, Logger);
        }
        return Task.FromResult(InitializeResult.Success);
    }

    private static bool HasScanPermissions() => OperatingSystem.IsAndroidVersionAtLeast(31)
        ? Application.Context.CheckSelfPermission(Manifest.Permission.BluetoothScan) is Permission.Granted
        : Application.Context.CheckSelfPermission(Manifest.Permission.AccessCoarseLocation) is Permission.Granted
          && Application.Context.CheckSelfPermission(Manifest.Permission.AccessFineLocation) is Permission.Granted;

    public override string Identifier => "Darp.Ble.Android";

    protected override void DisposeSyncInternal()
    {
        _bluetoothManager.Dispose();
    }
}