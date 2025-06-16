using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost;

/// <summary> Extension methods for easier addition of a <see cref="HciHostBleFactory"/> </summary>
public static class HciHostBleFactoryExtensions
{
    /// <summary>
    /// Add a new <see cref="HciHostBleFactory"/> and configure it.
    /// When requested, all serial ports will be enumerated for hci hosts
    /// </summary>
    /// <param name="builder"> An optional callback to configure the factory </param>
    /// <param name="configure"> The callback to configure the factory </param>
    /// <returns> The <paramref name="builder"/> </returns>
    public static BleManagerBuilder AddSerialHciHost(
        this BleManagerBuilder builder,
        Action<SerialHciHostBleFactory>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(configure);
    }

    /// <summary>
    /// Add a new <see cref="HciHostBleFactory"/> and configure it.
    /// When requested, a single hci host using the transport layer specified will be yielded.
    /// </summary>
    /// <param name="builder"> An optional callback to configure the factory </param>
    /// <param name="transportLayer"> The transport layer to be used </param>
    /// <param name="configure"> The callback to configure the factory </param>
    /// <returns> The <paramref name="builder"/> </returns>
    public static BleManagerBuilder AddHciHost(
        this BleManagerBuilder builder,
        ITransportLayer transportLayer,
        Action<HciHostBleFactory>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        var factory = new HciHostBleFactory(transportLayer);
        configure?.Invoke(factory);
        return builder.Add(factory);
    }

    /// <summary>
    /// Add a new <see cref="SingleSerialHciHostBleFactory"/> and configure it
    /// When requested, the specified <paramref name="portName"/> will be scanned for a hci host
    /// </summary>
    /// <param name="builder"> An optional callback to configure the factory </param>
    /// <param name="portName"> The port name to be scanned for a hci host </param>
    /// <param name="configure"> The callback to configure the factory </param>
    /// <returns> The <paramref name="builder"/> </returns>
    public static BleManagerBuilder AddSerialHciHost(
        this BleManagerBuilder builder,
        string portName,
        Action<SingleSerialHciHostBleFactory>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        var factory = new SingleSerialHciHostBleFactory(portName);
        configure?.Invoke(factory);
        return builder.Add(factory);
    }
}
