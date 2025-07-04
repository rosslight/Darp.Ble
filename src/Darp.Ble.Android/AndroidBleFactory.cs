using Android.Bluetooth;
using Android.Content.PM;

namespace Darp.Ble.Android;

/// <summary> A factory allowing to enumerate android devices </summary>
/// <param name="manager"> The Android <see cref="BluetoothManager"/> </param>
/// <exception cref="ArgumentNullException"> Thrown if the <paramref name="manager"/> is null </exception>
public sealed class AndroidBleFactory(BluetoothManager manager) : IBleFactory
{
    private readonly BluetoothManager _bluetoothManager =
        manager
        ?? throw new ArgumentNullException(nameof(manager), "The android bluetooth manager provided cannot be null.");

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(IServiceProvider serviceProvider)
    {
        if (_bluetoothManager.Adapter is null)
            yield break;
        if (Application.Context.PackageManager?.HasSystemFeature(PackageManager.FeatureBluetoothLe) == false)
            yield break;

        yield return new AndroidBleDevice(_bluetoothManager, serviceProvider);
    }
}
