using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Darp.BinaryObjects;
using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Verify;

namespace Darp.Ble.HciHost.Tests;

/// <summary> A central at the other end of a link to the adapter's peripheral role: sends ATT requests, gets the answers </summary>
internal sealed class FakeCentral
{
    private static readonly ConditionalWeakTable<FakeController, Router> Routers = [];

    private readonly FakeController _controller;
    private readonly Channel<byte[]> _received;

    public FakeCentral(FakeController controller, ushort handle, BleAddress? address = null)
    {
        _controller = controller;
        Handle = handle;
        Address = address ?? BleAddress.CreateRandomAddress((UInt48)(0xA1A2A3A4A500UL + handle));
        _received = Routers.GetValue(controller, c => new Router(c)).Add(handle);
    }

    public ushort Handle { get; }

    public BleAddress Address { get; }

    /// <summary> The notifications the host sent, in order </summary>
    public List<(ushort Handle, byte[] Value)> Notifications { get; } = [];

    /// <summary> Connects to the adapter; through an advertising set when its handle is given </summary>
    public async Task ConnectAsync(byte? advertisingHandle = null)
    {
        await _controller.PushAsync(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                Handle,
                HciLeConnectionRole.Peripheral,
                (byte)Address.Type,
                Address.Value,
                connectionInterval: 0x0028,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x01F4
            )
        );
        if (advertisingHandle is { } set)
        {
            // The controller stops a connectable set once a central connected through it
            await _controller.PushAsync(
                HciMessage.LeEventToHost([0x12, 0x00, set, (byte)Handle, (byte)(Handle >> 8), 0x00])
            );
        }
    }

    /// <summary> Sends a request and returns the host's answer; notifications on the way are collected </summary>
    public async Task<byte[]> RequestAsync(params byte[] pdu)
    {
        await SendAsync(pdu);
        return await NextAnswerAsync();
    }

    /// <summary> The next PDU from the host that is not a notification </summary>
    public async Task<byte[]> NextAnswerAsync()
    {
        while (true)
        {
            byte[] received = await _received.Reader.ReadAsync(TestContext.Current.CancellationToken);
            if (received[0] != (byte)AttOpCode.ATT_HANDLE_VALUE_NTF)
                return received;
            Notifications.Add((BinaryPrimitives.ReadUInt16LittleEndian(received.AsSpan(1)), received[3..]));
        }
    }

    /// <summary> The next notification from the host </summary>
    public async Task<(ushort Handle, byte[] Value)> NextNotificationAsync()
    {
        if (Notifications.Count > 0)
        {
            (ushort, byte[]) first = Notifications[0];
            Notifications.RemoveAt(0);
            return first;
        }
        byte[] received = await _received.Reader.ReadAsync(TestContext.Current.CancellationToken);
        return received[0] == (byte)AttOpCode.ATT_HANDLE_VALUE_NTF
            ? (BinaryPrimitives.ReadUInt16LittleEndian(received.AsSpan(1)), received[3..])
            : throw new InvalidOperationException($"Expected a notification, got {Convert.ToHexString(received)}");
    }

    /// <summary> Whether the host sent anything this central did not read yet </summary>
    public bool HasPending => _received.Reader.TryPeek(out _) || Notifications.Count > 0;

    /// <summary> Sends an ATT PDU to the host </summary>
    public Task SendAsync(params byte[] pdu)
    {
        var frame = new byte[4 + pdu.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)pdu.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), 0x0004);
        pdu.CopyTo(frame.AsSpan(4));
        var packet = new HciAclPacket(
            Handle,
            PacketBoundaryFlag.FirstAutoFlushable,
            BroadcastFlag.PointToPoint,
            (ushort)frame.Length,
            frame
        );
        return _controller.PushAsync(HciMessage.AclToHost(packet.ToArrayLittleEndian()));
    }

    /// <summary> The central takes the link down </summary>
    public Task DisconnectAsync(HciCommandStatus reason = HciCommandStatus.RemoteUserTerminatedConnection) =>
        _controller.PushAsync(HciMessages.HciDisconnectionCompleteEvent(Handle, reason));

    /// <summary> Reassembles the host's ACL packets per link </summary>
    private sealed class Router
    {
        private readonly Dictionary<ushort, Channel<byte[]>> _links = [];

        public Router(FakeController controller)
        {
            _ = RunAsync(controller);
        }

        public Channel<byte[]> Add(ushort handle)
        {
            var channel = Channel.CreateUnbounded<byte[]>();
            lock (_links)
                _links[handle] = channel;
            return channel;
        }

        private async Task RunAsync(FakeController controller)
        {
            var fragments = new Dictionary<ushort, List<byte>>();
            try
            {
                while (true)
                {
                    HciMessage acl = await controller.ReadAclAsync(CancellationToken.None);
                    byte[] packet = acl.PduBytes;
                    ushort header = BinaryPrimitives.ReadUInt16LittleEndian(packet);
                    var handle = (ushort)(header & 0x0FFF);
                    if (((header >> 12) & 0b11) is not (byte)PacketBoundaryFlag.ContinuingFragment)
                        fragments[handle] = [];
                    List<byte> frame = fragments[handle];
                    frame.AddRange(packet.AsSpan(4).ToArray());
                    if (frame.Count < 4 || frame.Count < 4 + BinaryPrimitives.ReadUInt16LittleEndian(frame.ToArray()))
                        continue;
                    fragments.Remove(handle);
                    Channel<byte[]>? link;
                    lock (_links)
                        _links.TryGetValue(handle, out link);
                    link?.Writer.TryWrite(frame.Skip(4).ToArray());
                }
            }
            catch (ChannelClosedException)
            {
                // The controller was disposed
            }
        }
    }
}
