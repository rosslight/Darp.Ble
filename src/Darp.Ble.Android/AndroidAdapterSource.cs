using Android;
using Android.Bluetooth;
using Android.Content.PM;
using Darp.Ble.Backends;

namespace Darp.Ble.Android;

/// <summary> The device's Bluetooth adapter </summary>
internal sealed class AndroidAdapterSource(BluetoothAdapter adapter) : IBleAdapterSource
{
    private readonly BluetoothAdapter _adapter = adapter;

    // The adapter's own name needs the BLUETOOTH_CONNECT permission, which a scanning app may not have
    public string? Name => "Android";

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Application.Context.CheckSelfPermission(Manifest.Permission.BluetoothScan) is not Permission.Granted)
            throw new BleOpenException(
                BleOpenErrorKind.MissingPermission,
                "The app has not been granted BLUETOOTH_SCAN"
            );
        if (!_adapter.IsEnabled)
            throw new BleOpenException(BleOpenErrorKind.Disabled, "Bluetooth is turned off", isTransient: true);
        return Task.FromResult<IBleAdapterDriver>(new AndroidAdapterDriver(_adapter, context.TimeProvider));
    }
}
