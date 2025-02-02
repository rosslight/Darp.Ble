namespace Darp.Ble.WinRT;

/// <summary> Extension methods for easier addition of a <see cref="WinBleFactory"/> </summary>
public static class WinBleFactoryExtensions
{
    /// <summary> Add a new <see cref="WinBleFactory"/> and configure it. </summary>
    /// <param name="builder"> An optional callback to configure the factory </param>
    /// <param name="configure"> The callback to configure the factory </param>
    /// <returns> The <paramref name="builder"/> </returns>
    public static BleManagerBuilder AddWinRT(this BleManagerBuilder builder, Action<WinBleFactory>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(configure);
    }
}
