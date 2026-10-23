using System.Buffers;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.Hci;

/// <summary>
/// Sends L2CAP frames in ACL packets as far as the controller's buffers allow.
/// One frame at a time is split into packets, so the packets of two frames never interleave. Links take turns at frame
/// boundaries, and answers of this host's ATT server go before other frames. A frame is handed off once its first packet
/// got a buffer; from then on it is sent completely while its link is up.
/// </summary>
/// <remarks>
/// One thread at a time sends: it takes packets under the lock and hands them off and to the transport outside it, so
/// the hand-off callbacks never run under the queue's lock and the packets keep their order. Frames come from the shared
/// array pool and go back once their last packet is sent: the transport copies each packet before
/// <see cref="ITransportLayer.Enqueue"/> returns
/// </remarks>
internal sealed class AclPacketQueue
{
    private readonly ITransportLayer _transportLayer;
    private readonly int _maxPacketsInFlight;

    // Guards the fields below and the links' frames: senders, the controller's completions and link ends all change them
    private readonly Lock _lock = new();
    private readonly List<Link> _links = [];

    // The link whose frame goes next, so the links take turns
    private int _nextLink;

    // The controller's ACL buffers this host may still fill
    private int _freeBuffers;

    // The frames waiting on all links, so the fast path needs no walk over them
    private int _queuedFrames;

    // Whether a thread sends; only one does, so the packets keep their order
    private bool _pumping;

    // The frame being split into packets; the next frame starts once it is sent completely
    private Frame? _sending;

    // The frame whose packet the sending thread hands to the transport outside the lock; it returns the bytes then
    private Frame? _transmitting;

    // Why the transport is gone, once it is
    private Exception? _closed;

    internal AclPacketQueue(ITransportLayer transportLayer, ushort maxPacketSize, int maxPacketsInFlight)
    {
        _transportLayer = transportLayer;
        MaxPacketSize = maxPacketSize;
        _maxPacketsInFlight = maxPacketsInFlight;
        _freeBuffers = maxPacketsInFlight;
    }

    /// <summary> The data bytes of one ACL packet </summary>
    public ushort MaxPacketSize { get; }

    /// <summary> A link came up; frames for it are accepted until <see cref="Flush"/> </summary>
    public void AddLink(ushort connectionHandle)
    {
        lock (_lock)
            _links.Add(new Link(connectionHandle));
    }

    /// <summary> Sends a frame of the link and waits until its first packet got a buffer </summary>
    /// <param name="connectionHandle"> The link </param>
    /// <param name="frame"> The L2CAP frame in an array of the shared pool; the queue owns it from here on </param>
    /// <param name="length"> The length of the frame </param>
    /// <param name="isAnswer"> Whether the frame answers a request of the peer and goes first </param>
    /// <param name="onHandOff"> Called when the first packet goes to the transport, before an answer to it can arrive. Must not block </param>
    /// <param name="cancellationToken"> Removes the frame unsent while it waits for a buffer </param>
    /// <param name="released"> Removes the frame unsent too, e.g. when its connection ends </param>
    /// <returns> A task that completes once the frame was handed off; synchronously when a buffer is free </returns>
    public ValueTask SendAsync(
        ushort connectionHandle,
        byte[] frame,
        int length,
        bool isAnswer,
        Action? onHandOff,
        CancellationToken cancellationToken,
        CancellationToken released
    )
    {
        Link? link;
        Frame? pending = null;
        TaskCompletionSource? handedOff = null;
        lock (_lock)
        {
            link = Find(connectionHandle);
            if (_closed is not null || link is null)
            {
                ArrayPool<byte>.Shared.Return(frame);
                return ValueTask.FromException(Closed(connectionHandle));
            }
            if (cancellationToken.IsCancellationRequested || released.IsCancellationRequested)
            {
                ArrayPool<byte>.Shared.Return(frame);
                return ValueTask.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : released);
            }
            // Nothing waits and the frame fits into one free buffer: this thread sends it, with no object for the wait
            bool sendsAtOnce =
                !_pumping && _sending is null && _queuedFrames == 0 && _freeBuffers > 0 && length <= MaxPacketSize;
            if (sendsAtOnce)
            {
                _pumping = true;
                TakeBuffer(link);
            }
            else
            {
                handedOff = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                pending = new Frame(this, link, frame, length, onHandOff, handedOff);
                Enqueue(pending, isAnswer);
            }
        }
        if (pending is null || handedOff is null)
        {
            SendNow(connectionHandle, frame, length, onHandOff);
            return ValueTask.CompletedTask;
        }
        Pump();
        // A token cancelled meanwhile runs the callback right here
        CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(
            static (state, token) => (state as Frame)?.Cancel(token),
            pending
        );
        CancellationTokenRegistration releasedRegistration = released.UnsafeRegister(
            static (state, token) => (state as Frame)?.Cancel(token),
            pending
        );
        bool waiting;
        lock (_lock)
        {
            waiting = !pending.IsHandedOff && pending.Node.List is not null;
            if (waiting)
            {
                pending.Registration = registration;
                pending.ReleasedRegistration = releasedRegistration;
            }
        }
        if (!waiting)
        {
            registration.Unregister();
            releasedRegistration.Unregister();
        }
        return new ValueTask(handedOff.Task);
    }

    /// <summary> Sends a one-packet frame on this thread, which took the sender's role and a buffer for it </summary>
    private void SendNow(ushort connectionHandle, byte[] frame, int length, Action? onHandOff)
    {
        onHandOff?.Invoke();
        Transmit(
            new HciAclPacket(
                connectionHandle,
                PacketBoundaryFlag.FirstNonAutoFlushable,
                BroadcastFlag.PointToPoint,
                (ushort)length,
                frame.AsMemory(0, length)
            )
        );
        ArrayPool<byte>.Shared.Return(frame);
        // Frames queued meanwhile waited for this thread
        PumpAsSender();
    }

    /// <summary> Sends a frame without waiting, e.g. an answer of this host's ATT server; dropped when its link is gone </summary>
    public void Post(ushort connectionHandle, byte[] frame, int length, bool isAnswer)
    {
        lock (_lock)
        {
            Link? link = Find(connectionHandle);
            if (_closed is not null || link is null)
            {
                ArrayPool<byte>.Shared.Return(frame);
                return;
            }
            Enqueue(new Frame(this, link, frame, length, onHandOff: null, handedOff: null), isAnswer);
        }
        Pump();
    }

    [Obsolete("Should be called by the HciHost only")]
    internal void OnHciNumberOfPacketsEvent(HciNumberOfCompletedPacketsEvent hciEvent)
    {
        lock (_lock)
        {
            foreach (HciNumberOfCompletedPackets completed in hciEvent.Handles)
            {
                // A link that went down got its buffers back already
                if (Find(completed.ConnectionHandle) is not { } link)
                    continue;
                int freed = Math.Min(completed.NumCompletedPackets, link.InFlight);
                link.InFlight -= freed;
                _freeBuffers = Math.Min(_maxPacketsInFlight, _freeBuffers + freed);
            }
        }
        Pump();
    }

    /// <summary> The link went down: its frames fail, and the controller freed its buffers </summary>
    public void Flush(ushort connectionHandle, Exception reason)
    {
        List<Frame> dropped;
        lock (_lock)
        {
            if (Find(connectionHandle) is not { } link)
                return;
            int index = _links.IndexOf(link);
            _links.RemoveAt(index);
            if (_nextLink > index)
                _nextLink--;
            _freeBuffers = Math.Min(_maxPacketsInFlight, _freeBuffers + link.InFlight);
            dropped = Drop(link);
        }
        Fail(dropped, reason);
        Pump();
    }

    /// <summary> The transport is gone: every frame fails, and no new one is accepted </summary>
    public void Close(Exception reason)
    {
        List<Frame> dropped = [];
        lock (_lock)
        {
            if (_closed is not null)
                return;
            _closed = reason;
            foreach (Link link in _links)
                dropped.AddRange(Drop(link));
            _links.Clear();
        }
        Fail(dropped, reason);
    }

    private void Enqueue(Frame frame, bool isAnswer)
    {
        (isAnswer ? frame.Link.Answers : frame.Link.Frames).AddLast(frame.Node);
        _queuedFrames++;
    }

    private void TakeBuffer(Link link)
    {
        link.InFlight++;
        _freeBuffers--;
    }

    /// <summary> Becomes the sending thread unless another one is </summary>
    private void Pump()
    {
        lock (_lock)
        {
            if (_pumping)
                return;
            _pumping = true;
        }
        PumpAsSender();
    }

    /// <summary> Sends packets while there are buffers: first the rest of the current frame, then the next frame </summary>
    private void PumpAsSender()
    {
        while (true)
        {
            Frame frame;
            HciAclPacket packet;
            bool first;
            bool last;
            lock (_lock)
            {
                if (_closed is not null || _freeBuffers == 0 || (_sending is null && _queuedFrames == 0))
                {
                    _pumping = false;
                    return;
                }
                if (_sending is null)
                {
                    _sending = Next();
                    _sending.MarkHandedOff();
                }
                frame = _sending;
                int count = Math.Min(MaxPacketSize, frame.Length - frame.Offset);
                first = frame.Offset == 0;
                packet = new HciAclPacket(
                    frame.Link.ConnectionHandle,
                    first ? PacketBoundaryFlag.FirstNonAutoFlushable : PacketBoundaryFlag.ContinuingFragment,
                    BroadcastFlag.PointToPoint,
                    (ushort)count,
                    frame.Bytes.AsMemory(frame.Offset, count)
                );
                TakeBuffer(frame.Link);
                frame.Offset += count;
                last = frame.Offset >= frame.Length;
                if (last)
                    _sending = null;
                // A link that goes down meanwhile leaves the frame's bytes to this thread
                _transmitting = frame;
            }
            if (first)
                frame.CompleteHandOff();
            Transmit(packet);
            bool returnBytes;
            lock (_lock)
            {
                _transmitting = null;
                returnBytes = last || frame.IsDropped;
            }
            if (returnBytes)
                ArrayPool<byte>.Shared.Return(frame.Bytes);
        }
    }

    private void Transmit(HciAclPacket packet)
    {
        try
        {
            _transportLayer.Enqueue(packet);
        }
        catch (Exception e)
        {
            // A broken transport fails the links; its owner learns it through the transport's own error
            Close(e);
        }
    }

    /// <summary> The next frame: answers first, and the links take turns </summary>
    private Frame Next()
    {
        foreach (bool answers in (ReadOnlySpan<bool>)[true, false])
        {
            for (var i = 0; i < _links.Count; i++)
            {
                int index = (_nextLink + i) % _links.Count;
                LinkedList<Frame> frames = answers ? _links[index].Answers : _links[index].Frames;
                if (frames.First is not { } first)
                    continue;
                frames.RemoveFirst();
                _queuedFrames--;
                _nextLink = (index + 1) % _links.Count;
                return first.Value;
            }
        }
        throw new InvalidOperationException("A frame was queued but none was found");
    }

    /// <summary> Removes a frame that still waits for a buffer </summary>
    private bool TryRemove(Frame frame)
    {
        lock (_lock)
        {
            if (frame.IsHandedOff || frame.Node.List is not { } list)
                return false;
            list.Remove(frame.Node);
            _queuedFrames--;
            // Both tokens may live much longer than the frame, e.g. the connection's
            frame.Registration.Unregister();
            frame.ReleasedRegistration.Unregister();
        }
        ArrayPool<byte>.Shared.Return(frame.Bytes);
        return true;
    }

    /// <summary> Takes the link's frames out of the queue and returns their bytes, except those the sending thread holds </summary>
    private List<Frame> Drop(Link link)
    {
        List<Frame> dropped = [];
        foreach (LinkedList<Frame> frames in (ReadOnlySpan<LinkedList<Frame>>)[link.Answers, link.Frames])
        {
            dropped.AddRange(frames);
            _queuedFrames -= frames.Count;
            frames.Clear();
        }
        foreach (Frame frame in dropped)
            ArrayPool<byte>.Shared.Return(frame.Bytes);
        // The rest of a frame that is half sent belongs to the link that is gone
        if (_sending?.Link == link)
        {
            Frame sending = _sending;
            _sending = null;
            sending.IsDropped = true;
            if (!ReferenceEquals(_transmitting, sending))
                ArrayPool<byte>.Shared.Return(sending.Bytes);
        }
        return dropped;
    }

    private static void Fail(List<Frame> frames, Exception reason)
    {
        foreach (Frame frame in frames)
            frame.Fail(reason);
    }

    private Link? Find(ushort connectionHandle)
    {
        foreach (Link link in _links)
        {
            if (link.ConnectionHandle == connectionHandle)
                return link;
        }
        return null;
    }

    private HciException Closed(ushort connectionHandle) =>
        _closed is not null
            ? new HciException("The HCI transport is gone", _closed)
            : new HciException($"The link 0x{connectionHandle:X3} is down");

    private sealed class Link(ushort connectionHandle)
    {
        public ushort ConnectionHandle { get; } = connectionHandle;
        public LinkedList<Frame> Answers { get; } = new();
        public LinkedList<Frame> Frames { get; } = new();
        public int InFlight { get; set; }
    }

    private sealed class Frame
    {
        private readonly AclPacketQueue _queue;
        private readonly Action? _onHandOff;
        private readonly TaskCompletionSource? _handedOff;

        public Frame(
            AclPacketQueue queue,
            Link link,
            byte[] bytes,
            int length,
            Action? onHandOff,
            TaskCompletionSource? handedOff
        )
        {
            _queue = queue;
            _onHandOff = onHandOff;
            _handedOff = handedOff;
            Link = link;
            Bytes = bytes;
            Length = length;
            Node = new LinkedListNode<Frame>(this);
        }

        public Link Link { get; }
        public byte[] Bytes { get; }
        public int Length { get; }
        public int Offset { get; set; }
        public LinkedListNode<Frame> Node { get; }

        /// <summary> Set under the queue's lock once the frame left the queue for the transport </summary>
        public bool IsHandedOff { get; private set; }

        /// <summary> Set under the queue's lock when its link went down while it was half sent </summary>
        public bool IsDropped { get; set; }

        public CancellationTokenRegistration Registration { get; set; }

        public CancellationTokenRegistration ReleasedRegistration { get; set; }

        public void MarkHandedOff() => IsHandedOff = true;

        /// <summary> Outside the queue's lock: tells the caller its frame is on its way </summary>
        public void CompleteHandOff()
        {
            _onHandOff?.Invoke();
            Registration.Unregister();
            ReleasedRegistration.Unregister();
            _handedOff?.TrySetResult();
        }

        public void Fail(Exception reason)
        {
            Registration.Unregister();
            ReleasedRegistration.Unregister();
            _handedOff?.TrySetException(reason);
        }

        public void Cancel(CancellationToken cancellationToken)
        {
            if (_queue.TryRemove(this))
                _handedOff?.TrySetCanceled(cancellationToken);
        }
    }
}
