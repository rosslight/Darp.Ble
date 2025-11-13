using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost;

/// <summary> A factory searching for a single, specified hci host </summary>
/// <param name="transportLayer"> The transport layer to use for the HCI Host </param>
public sealed class HciHostBleFactory(ITransportLayer transportLayer) : IHciHostBleFactory
{
    private readonly ITransportLayer _transportLayer = transportLayer;

    /// <summary> A hardcoded device name </summary>
    public string? DeviceName { get; set; }

    /// <summary> The random address of the device </summary>
    public BleAddress? RandomAddress { get; set; }

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary> Settings to be used by devices enumerated by this factory </summary>
    public HciSettings Settings { get; set; } = HciSettings.Default;

    /// <inheritdoc />
    IEnumerable<IBleDevice> IBleFactory.EnumerateDevices(IServiceProvider serviceProvider)
    {
        yield return new HciHostBleDevice(
            DeviceName,
            randomAddress: RandomAddress,
            _transportLayer,
            Settings,
            serviceProvider
        );
    }
}
