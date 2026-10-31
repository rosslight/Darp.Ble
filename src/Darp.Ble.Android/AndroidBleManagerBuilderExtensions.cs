namespace Darp.Ble.Android;

/// <summary> Registers the Android backend </summary>
public static class AndroidBleManagerBuilderExtensions
{
    /// <summary> Adds the device's Bluetooth adapter, for scanning; a device without Bluetooth Low Energy adds none </summary>
    /// <param name="builder"> The builder </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddAndroid(this BleManagerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddBackend(new AndroidBackend());
    }
}
