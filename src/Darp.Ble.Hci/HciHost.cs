using System.Reactive.Linq;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble.Hci;

public sealed class HciHost : IDisposable
{
    private readonly ITransportLayer _transportLayer;
    internal ILogger<HciHost> Logger { get; }

    public HciHost(ITransportLayer transportLayer, ILogger<HciHost>? logger)
    {
        _transportLayer = transportLayer;
        Logger = logger ?? NullLogger<HciHost>.Instance;
        WhenHciPacketReceived = _transportLayer.WhenReceived();
        WhenHciEventPackageReceived = WhenHciPacketReceived.OfType<HciEventPacket>();
        WhenHciLeMetaEventPackageReceived = WhenHciEventPackageReceived
            .SelectWhereEvent<HciLeMetaEvent>();
    }

    public IObservable<IHciPacket> WhenHciPacketReceived { get; }
    public IObservable<HciEventPacket> WhenHciEventPackageReceived { get; }
    public IObservable<HciEventPacket<HciLeMetaEvent>> WhenHciLeMetaEventPackageReceived { get; }

    public void EnqueuePacket(IHciPacket packet)
    {
        Logger.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    public void Initialize()
    {
        _transportLayer.Initialize();
    }

    public void Dispose() => _transportLayer.Dispose();
}