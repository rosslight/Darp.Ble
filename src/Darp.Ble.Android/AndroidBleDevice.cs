using System.Diagnostics.CodeAnalysis;
using Android;
using Android.Bluetooth;
using Android.Content.PM;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Android;

public sealed class AndroidBleDevice(BluetoothManager bluetoothManager, ILoggerFactory loggerFactory)
    : BleDevice(loggerFactory, loggerFactory.CreateLogger<AndroidBleDevice>())
{
    private readonly BluetoothManager _bluetoothManager = bluetoothManager;
    private BluetoothAdapter? BluetoothAdapter => _bluetoothManager.Adapter;

    [MemberNotNullWhen(true, nameof(BluetoothAdapter))]
    public bool IsAvailable => BluetoothAdapter?.IsEnabled == true;

    public override string? Name => BluetoothAdapter?.Name;
    public override AppearanceValues Appearance => AppearanceValues.Unknown;

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    protected override Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        if (BluetoothAdapter is null)
            return Task.FromResult(InitializeResult.DeviceNotAvailable);
        if (!BluetoothAdapter.IsEnabled)
            return Task.FromResult(InitializeResult.DeviceNotEnabled);
        if (!IsAvailable)
            return Task.FromResult(InitializeResult.DeviceNotAvailable);

        if (HasScanPermissions() && BluetoothAdapter.BluetoothLeScanner is not null)
        {
            Observer = new AndroidBleObserver(
                this,
                BluetoothAdapter.BluetoothLeScanner,
                LoggerFactory.CreateLogger<AndroidBleObserver>()
            );
        }
        return Task.FromResult(InitializeResult.Success);
    }

    private static bool HasScanPermissions() =>
        OperatingSystem.IsAndroidVersionAtLeast(31)
            ? Application.Context.CheckSelfPermission(Manifest.Permission.BluetoothScan) is Permission.Granted
            : Application.Context.CheckSelfPermission(Manifest.Permission.AccessCoarseLocation) is Permission.Granted
                && Application.Context.CheckSelfPermission(Manifest.Permission.AccessFineLocation)
                    is Permission.Granted;

    public override string Identifier => BleDeviceIdentifiers.Android;

    protected override void Dispose(bool disposing)
    {
        _bluetoothManager.Dispose();
        base.Dispose(disposing);
    }
}
