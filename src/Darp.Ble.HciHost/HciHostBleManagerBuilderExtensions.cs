using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Transport;
using Darp.Ble.HciHost.Usb;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> Registers the HCI backend </summary>
[SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The port name tells the overloads apart; configuring is optional in both"
)]
public static class HciHostBleManagerBuilderExtensions
{
    /// <summary> Adds an adapter for every USB serial port whose device is listed in <see cref="SerialHciHostOptions.DeviceNameMapping"/> </summary>
    /// <param name="builder"> The builder </param>
    /// <param name="configure"> Configures the options </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddSerialHciHost(
        this BleManagerBuilder builder,
        Action<SerialHciHostOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new SerialHciHostOptions();
        configure?.Invoke(options);
        var mapping = new Dictionary<(ushort VendorId, ushort ProductId), string>(options.DeviceNameMapping);
        return builder.AddBackend(
            new HciHostBackend(() =>
                [
                    .. UsbPort
                        .GetPortInfos()
                        .Where(port => port.Port is not null && mapping.ContainsKey((port.VendorId, port.ProductId)))
                        .Select(port =>
                            SerialSource(
                                port.Port ?? "",
                                $"{mapping[(port.VendorId, port.ProductId)]} ({port.Port})",
                                options.RandomAddress,
                                options.Settings
                            )
                        ),
                ]
            )
        );
    }

    /// <summary> Adds the adapter on the given serial port </summary>
    /// <param name="builder"> The builder </param>
    /// <param name="portName"> The serial port, e.g. COM3 or /dev/ttyACM0 </param>
    /// <param name="configure"> Configures the options </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddSerialHciHost(
        this BleManagerBuilder builder,
        string portName,
        Action<HciHostOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(portName);
        var options = new HciHostOptions();
        configure?.Invoke(options);
        HciHostAdapterSource source = SerialSource(
            portName,
            options.Name ?? portName,
            options.RandomAddress,
            options.Settings
        );
        return builder.AddBackend(new HciHostBackend(() => [source]));
    }

    /// <summary> Adds an adapter on a transport of your own, e.g. HCI over a network bridge </summary>
    /// <param name="builder"> The builder </param>
    /// <param name="transportLayer"> The transport. The adapter owns it once opened and disposes it with itself, so it opens once </param>
    /// <param name="configure"> Configures the options </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddHciHost(
        this BleManagerBuilder builder,
        ITransportLayer transportLayer,
        Action<HciHostOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(transportLayer);
        var options = new HciHostOptions();
        configure?.Invoke(options);
        var source = new HciHostAdapterSource(
            options.Name,
            _ => transportLayer,
            options.RandomAddress,
            options.Settings
        );
        return builder.AddBackend(new HciHostBackend(() => [source]));
    }

    private static HciHostAdapterSource SerialSource(
        string portName,
        string name,
        Data.BleAddress? randomAddress,
        Hci.HciSettings settings
    ) =>
        new(
            name,
            context => new H4TransportLayer(portName, context.LoggerFactory.CreateLogger<H4TransportLayer>()),
            randomAddress,
            settings
        );
}
