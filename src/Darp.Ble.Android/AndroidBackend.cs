using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Darp.Ble.Backends;

namespace Darp.Ble.Android;

/// <summary> Android's Bluetooth stack: the device's adapter, when it supports Low Energy </summary>
internal sealed class AndroidBackend : IBleBackend
{
    public string Name => BleBackends.Android;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Context app = Application.Context;
        if (
            app.PackageManager?.HasSystemFeature(PackageManager.FeatureBluetoothLe) is not true
            || app.GetSystemService(Context.BluetoothService) is not BluetoothManager { Adapter: { } adapter }
        )
            return Task.FromResult<IReadOnlyList<IBleAdapterSource>>([]);
        return Task.FromResult<IReadOnlyList<IBleAdapterSource>>([new AndroidAdapterSource(adapter)]);
    }
}
