namespace Darp.Ble.WinRT;

/// <summary> Registers the Windows backend </summary>
public static class WinRTBleManagerBuilderExtensions
{
    /// <summary> Adds Windows' default Bluetooth adapter. On other platforms, or a build without the Windows target, it adds no adapter </summary>
    /// <param name="builder"> The builder </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddWinRT(this BleManagerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddBackend(new WinRTBackend());
    }
}
