using System.Reactive;
using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

/// <summary> The gatt client peer </summary>
public abstract class GattClientPeer(
    BlePeripheral peripheral,
    BleAddress address,
    ILogger<GattClientPeer> logger
) : IGattClientPeer
{
    /// <inheritdoc />
    public IBlePeripheral Peripheral { get; } = peripheral;

    /// <inheritdoc />
    public BleAddress Address { get; } = address;

    /// <summary> The logger </summary>
    protected ILogger<GattClientPeer> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Peripheral.Device.LoggerFactory;

    /// <inheritdoc />
    public abstract bool IsConnected { get; }

    /// <inheritdoc />
    public abstract IObservable<Unit> WhenDisconnected { get; }
}
