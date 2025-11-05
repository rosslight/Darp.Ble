using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.Hci;

/// <summary> Defines an acl packet queue </summary>
public interface IAclPacketQueue
{
    /// <summary> The maximum packet size that was configured </summary>
    ushort MaxPacketSize { get; }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="aclPacket"> The ACL packet to be enqueued </param>
    void Enqueue(HciAclPacket aclPacket);

    internal void Flush(ushort connectionHandle);
}

/// <summary> The acl packet queue </summary>
internal sealed class AclPacketQueue : IAclPacketQueue
{
    private readonly ITransportLayer _transportLayer;
    private readonly int _maxPacketsInFlight;
    private readonly Dictionary<ushort, ConnectionState> _packetQueues = [];
    private readonly Lock _lock = new();
    private int _packetsInFlight;

    /// <inheritdoc />
    public ushort MaxPacketSize { get; }

    internal AclPacketQueue(ITransportLayer transportLayer, ushort maxPacketSize, int maxPacketsInFlight)
    {
        _transportLayer = transportLayer;
        MaxPacketSize = maxPacketSize;
        _maxPacketsInFlight = maxPacketsInFlight;
    }

    [Obsolete("Should be called by the HciHost only")]
    internal void OnHciNumberOfPacketsEvent(HciNumberOfCompletedPacketsEvent hciEvent)
    {
        lock (_lock)
        {
            foreach (HciNumberOfCompletedPackets evt in hciEvent.Handles)
            {
                if (_packetQueues.TryGetValue(evt.ConnectionHandle, out ConnectionState? connectionState))
                {
                    int packetsToFree = Math.Min(evt.NumCompletedPackets, connectionState.InFlight);
                    connectionState.InFlight -= packetsToFree;
                    _packetsInFlight = Math.Max(0, _packetsInFlight - packetsToFree);
                }
                else
                {
                    _packetsInFlight = Math.Max(0, _packetsInFlight - evt.NumCompletedPackets);
                }
            }
        }
        CheckQueue();
    }

    /// <inheritdoc />
    public void Enqueue(HciAclPacket aclPacket)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(aclPacket.GetByteCount(), MaxPacketSize);
        ushort connectionHandle = aclPacket.ConnectionHandle;
        lock (_lock)
        {
            if (!_packetQueues.TryGetValue(connectionHandle, out ConnectionState? connectionState))
            {
                connectionState = new ConnectionState();
                _packetQueues[connectionHandle] = connectionState;
            }
            connectionState.Queue.Enqueue(aclPacket);
        }
        CheckQueue();
    }

    /// <summary> Checks the number of packets in flight and send as many as possible </summary>
    private void CheckQueue()
    {
        while (true)
        {
            HciAclPacket? pkt;
            lock (_lock)
            {
                if (_packetsInFlight >= _maxPacketsInFlight)
                    return;
                if (!TryDequeueFirstPacket(out ConnectionState? state, out pkt))
                    return;
                state.InFlight++;
                _packetsInFlight++;
            }
            _transportLayer.Enqueue(pkt);
        }
    }

    /// <summary> Tries to dequeue a package from the queue. </summary>
    /// <param name="connectionState"> The connectionState the packet is associated with </param>
    /// <param name="aclPacket"> The packet that was just dequeued </param>
    /// <returns> True, if a packet was dequeued and the out params were set. False, otherwise </returns>
    private bool TryDequeueFirstPacket(
        [NotNullWhen(true)] out ConnectionState? connectionState,
        [NotNullWhen(true)] out HciAclPacket? aclPacket
    )
    {
        foreach ((_, ConnectionState value) in _packetQueues)
        {
            if (!value.Queue.TryDequeue(out HciAclPacket dequeuedPkt))
                continue;
            connectionState = value;
            aclPacket = dequeuedPkt;
            return true;
        }
        connectionState = null;
        aclPacket = null;
        return false;
    }

    /// <summary>Flush queued packets for a connection and reclaim its in-flight credits.</summary>
    public void Flush(ushort connectionHandle)
    {
        ConnectionState? removed;

        lock (_lock)
        {
            if (_packetQueues.Remove(connectionHandle, out removed))
            {
                int reclaimed = Math.Min(removed.InFlight, _packetsInFlight);
                _packetsInFlight -= reclaimed;
                removed.InFlight = 0;
            }
        }

        if (removed is not null)
            CheckQueue();
    }
}

internal sealed class ConnectionState
{
    public Queue<HciAclPacket> Queue { get; } = new();
    public int InFlight { get; set; }
}
