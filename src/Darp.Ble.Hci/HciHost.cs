using System.Reactive.Linq;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary> The HCI Host </summary>
public sealed class HciHost : IDisposable
{
    private readonly ITransportLayer _transportLayer;
    internal ILogger? Logger { get; }

    /// <summary> Initializes a new host with a given transport layer and an optional logger </summary>
    /// <param name="transportLayer"> The transport layer </param>
    /// <param name="logger"> An optional logger </param>
    public HciHost(ITransportLayer transportLayer, ILogger? logger)
    {
        _transportLayer = transportLayer;
        Logger = logger;
        WhenHciPacketReceived = _transportLayer.WhenReceived();
        WhenHciEventPackageReceived = WhenHciPacketReceived.OfType<HciEventPacket>();
        WhenHciLeMetaEventPackageReceived = WhenHciEventPackageReceived
            .SelectWhereEvent<HciLeMetaEvent>();
    }

    /// <summary> Observable sequence of <see cref="IHciPacket"/> emitted when an HCI packet is received. </summary>
    public IObservable<IHciPacket> WhenHciPacketReceived { get; }
    /// <summary> Observable sequence of <see cref="HciEventPacket"/> emitted when an HCI event packet is received. </summary>
    public IObservable<HciEventPacket> WhenHciEventPackageReceived { get; }
    /// <summary> Observable sequence of <see cref="HciEventPacket{HciLeMetaEvent}"/> emitted when an HCI event packet with <see cref="HciLeMetaEvent"/> is received. </summary>
    public IObservable<HciEventPacket<HciLeMetaEvent>> WhenHciLeMetaEventPackageReceived { get; }

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    public void EnqueuePacket(IHciPacket packet)
    {
        Logger?.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    /// <summary> Initialize the host </summary>
    public void Initialize() => _transportLayer.Initialize();

    /// <inheritdoc />
    public void Dispose() => _transportLayer.Dispose();
}