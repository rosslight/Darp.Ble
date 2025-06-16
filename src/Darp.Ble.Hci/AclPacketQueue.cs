using System.Collections.Concurrent;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;
using Darp.Utils.Messaging;

namespace Darp.Ble.Hci;

/// <summary> Defines an acl packet queue </summary>
public interface IAclPacketQueue
{
    /// <summary> The maximum packet size that was configured </summary>
    ushort MaxPacketSize { get; }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="aclPacket"> The ACL packet to be enqueued </param>
    void Enqueue(HciAclPacket aclPacket);
}

/// <summary> The acl packet queue </summary>
internal sealed partial class AclPacketQueue : IAclPacketQueue, IDisposable
{
    private readonly ITransportLayer _transportLayer;
    private readonly int _maxPacketsInFlight;
    private readonly ConcurrentQueue<HciAclPacket> _packetQueue = [];
    private readonly IDisposable _subscription;
    private int _packetsInFlight;

    /// <inheritdoc />
    public ushort MaxPacketSize { get; }

    internal AclPacketQueue(HciHost host, ITransportLayer transportLayer, ushort maxPacketSize, int maxPacketsInFlight)
    {
        _transportLayer = transportLayer;
        MaxPacketSize = maxPacketSize;
        _maxPacketsInFlight = maxPacketsInFlight;
        _subscription = host.Subscribe(this);
    }

    [MessageSink]
    private void OnHciNumberOfPacketsEvent(HciNumberOfCompletedPacketsEvent hciEvent)
    {
        foreach (HciNumberOfCompletedPackets hciNumberOfCompletedPackets in hciEvent.Handles)
        {
            _packetsInFlight -= hciNumberOfCompletedPackets.NumCompletedPackets;
            CheckQueue();
        }
    }

    /// <inheritdoc />
    public void Enqueue(HciAclPacket aclPacket)
    {
        _packetQueue.Enqueue(aclPacket);
        CheckQueue();
    }

    /// <summary> Checks the number of packets in flight and send as many as possible </summary>
    private void CheckQueue()
    {
        while (_packetsInFlight < _maxPacketsInFlight && _packetQueue.TryDequeue(out HciAclPacket packet))
        {
            _transportLayer.Enqueue(packet);
            _packetsInFlight++;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();
}
