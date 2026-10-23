using System.Buffers.Binary;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;

namespace Darp.Ble.HciHost.Tests;

/// <summary> The GATT server of a peripheral at the other end of a link, answering from an attribute table </summary>
internal sealed class FakePeripheral(FakeController controller, ushort handle)
{
    private readonly FakeController _controller = controller;
    private readonly List<Attribute> _attributes = [];
    private readonly Queue<HciMessage> _held = new();
    private readonly Dictionary<ushort, AttErrorCode> _errors = [];
    private readonly Lock _lock = new();
    private TaskCompletionSource _received = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ushort Handle { get; } = handle;

    /// <summary> The MTU this peripheral agrees on </summary>
    public ushort Mtu { get; init; } = 65;

    /// <summary> Whether answers wait for <see cref="ReleaseAsync"/> </summary>
    public bool HoldAnswers { get; set; }

    /// <summary> The ATT PDUs the host sent on this link, reassembled, in order </summary>
    public List<byte[]> Received { get; } = [];

    /// <summary> Adds a primary service; returns its handles through the builder </summary>
    public FakePeripheral AddService(BleUuid uuid, Action<ServiceBuilder> build)
    {
        var start = (ushort)(_attributes.Count + 1);
        var declaration = new Attribute(start, 0x2800, UuidBytes(uuid));
        _attributes.Add(declaration);
        build(new ServiceBuilder(this));
        declaration.GroupEnd = (ushort)_attributes.Count;
        return this;
    }

    /// <summary> Waits until the received PDUs fulfil the condition </summary>
    public async Task WaitForReceivedAsync(Func<IReadOnlyList<byte[]>, bool> condition)
    {
        while (true)
        {
            Task received;
            lock (_lock)
            {
                if (condition(Received))
                    return;
                received = _received.Task;
            }
            await received;
        }
    }

    /// <summary> The value of an attribute, e.g. after the host wrote it </summary>
    public byte[] ValueOf(ushort attributeHandle) => _attributes[attributeHandle - 1].Value;

    /// <summary> Answers every request for the attribute with the given error </summary>
    public void Fail(ushort attributeHandle, AttErrorCode error) => _errors[attributeHandle] = error;

    /// <summary> Sends the oldest held answer </summary>
    public Task ReleaseAsync()
    {
        HciMessage answer;
        lock (_lock)
            answer = _held.Dequeue();
        return _controller.PushAsync(answer);
    }

    /// <summary> Sends a notification </summary>
    public Task NotifyAsync(ushort valueHandle, byte[] value) =>
        _controller.PushAsync(
            HciMessages.AttToHost(Handle, new AttHandleValueNtf { Handle = valueHandle, Value = value })
        );

    /// <summary> Takes the link down from the peripheral's side </summary>
    public Task DisconnectAsync(HciCommandStatus reason = HciCommandStatus.RemoteUserTerminatedConnection) =>
        _controller.PushAsync(HciMessages.HciDisconnectionCompleteEvent(Handle, reason));

    /// <summary> Answers the ATT requests of every link until the controller is disposed </summary>
    public static async Task ServeAsync(FakeController controller, params FakePeripheral[] peripherals)
    {
        var fragments = new Dictionary<ushort, List<byte>>();
        try
        {
            while (true)
            {
                HciMessage acl = await controller.ReadAclAsync(CancellationToken.None);
                byte[] packet = acl.PduBytes;
                ushort header = BinaryPrimitives.ReadUInt16LittleEndian(packet);
                var linkHandle = (ushort)(header & 0x0FFF);
                bool continuing = ((header >> 12) & 0b11) is (byte)PacketBoundaryFlag.ContinuingFragment;
                if (!continuing)
                    fragments[linkHandle] = [];
                List<byte> frame = fragments[linkHandle];
                frame.AddRange(packet.AsSpan(4).ToArray());
                if (frame.Count < 4 || frame.Count < 4 + BinaryPrimitives.ReadUInt16LittleEndian(frame.ToArray()))
                    continue;
                byte[] pdu = frame.Skip(4).ToArray();
                fragments.Remove(linkHandle);
                FakePeripheral? peripheral = peripherals.FirstOrDefault(p => p.Handle == linkHandle);
                if (peripheral is not null)
                    await peripheral.OnPduAsync(pdu);
            }
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            // The controller was disposed
        }
    }

    private async Task OnPduAsync(byte[] pdu)
    {
        HciMessage? answer = Answer(pdu);
        TaskCompletionSource received;
        lock (_lock)
        {
            Received.Add(pdu);
            received = _received;
            _received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        received.TrySetResult();
        if (answer is null)
            return;
        lock (_lock)
        {
            if (HoldAnswers)
            {
                _held.Enqueue(answer);
                return;
            }
        }
        await _controller.PushAsync(answer);
    }

    private HciMessage? Answer(byte[] pdu)
    {
        var opCode = (AttOpCode)pdu[0];
        ReadOnlySpan<byte> body = pdu.AsSpan(1);
        switch (opCode)
        {
            case AttOpCode.ATT_EXCHANGE_MTU_REQ:
                return HciMessages.AttExchangeMtuResponse(Handle, Mtu);
            case AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ:
            {
                (ushort start, ushort end) = Range(body);
                ushort groupType = ReadUInt16(body[4..]);
                AttGroupTypeData[] services =
                [
                    .. InRange(start, end)
                        .Where(a => a.Type == groupType)
                        .GroupBy(a => a.Value.Length)
                        .FirstOrDefault()
                        ?.Select(a => new AttGroupTypeData(a.Handle, a.GroupEnd, a.Value))
                        .Take((Mtu - 2) / (4 + 2))
                        ?? [],
                ];
                return services.Length > 0
                    ? HciMessages.AttReadByGroupTypeResponse(Handle, services)
                    : HciMessages.AttNotFoundErrorResponse(Handle, opCode, start);
            }
            case AttOpCode.ATT_READ_BY_TYPE_REQ:
            {
                (ushort start, ushort end) = Range(body);
                ushort type = ReadUInt16(body[4..]);
                Attribute[] matches = [.. InRange(start, end).Where(a => a.Type == type)];
                AttReadByTypeData[] data =
                [
                    .. matches
                        .TakeWhile(a => a.Value.Length == matches[0].Value.Length)
                        .Select(a => new AttReadByTypeData(a.Handle, a.Value))
                        .Take(Math.Max(1, (Mtu - 2) / (2 + (matches.Length > 0 ? matches[0].Value.Length : 1)))),
                ];
                return data.Length > 0
                    ? HciMessages.AttReadByTypeResponse(Handle, data)
                    : HciMessages.AttNotFoundErrorResponse(Handle, opCode, start);
            }
            case AttOpCode.ATT_FIND_INFORMATION_REQ:
            {
                (ushort start, ushort end) = Range(body);
                AttFindInformationData[] data =
                [
                    .. InRange(start, end)
                        .Take((Mtu - 2) / 4)
                        .Select(a => new AttFindInformationData(a.Handle, BitConverter.GetBytes(a.Type))),
                ];
                return data.Length > 0
                    ? HciMessages.AttFindInformationResponse(Handle, data)
                    : HciMessages.AttNotFoundErrorResponse(Handle, opCode, start);
            }
            case AttOpCode.ATT_READ_REQ
            or AttOpCode.ATT_WRITE_REQ when _errors.TryGetValue(ReadUInt16(body), out AttErrorCode error):
                return HciMessages.AttErrorResponse(Handle, opCode, ReadUInt16(body), error);
            case AttOpCode.ATT_READ_REQ:
            {
                ushort attributeHandle = ReadUInt16(body);
                if (attributeHandle == 0 || attributeHandle > _attributes.Count)
                    return HciMessages.AttErrorResponse(Handle, opCode, attributeHandle, AttErrorCode.InvalidHandle);
                byte[] value = _attributes[attributeHandle - 1].Value;
                return HciMessages.AttToHost(
                    Handle,
                    new AttReadRsp { AttributeValue = value.AsMemory(0, Math.Min(value.Length, Mtu - 1)) }
                );
            }
            case AttOpCode.ATT_WRITE_REQ:
            {
                ushort attributeHandle = ReadUInt16(body);
                if (attributeHandle == 0 || attributeHandle > _attributes.Count)
                    return HciMessages.AttErrorResponse(Handle, opCode, attributeHandle, AttErrorCode.InvalidHandle);
                _attributes[attributeHandle - 1].Value = body[2..].ToArray();
                return HciMessages.AttToHost(Handle, new AttWriteRsp());
            }
            case AttOpCode.ATT_WRITE_CMD:
            {
                ushort attributeHandle = ReadUInt16(body);
                if (attributeHandle > 0 && attributeHandle <= _attributes.Count)
                    _attributes[attributeHandle - 1].Value = body[2..].ToArray();
                return null;
            }
            default:
                // Answers of the peripheral's own requests, e.g. the host's MTU response
                return null;
        }
    }

    private IEnumerable<Attribute> InRange(ushort start, ushort end) =>
        _attributes.Where(a => a.Handle >= start && a.Handle <= end);

    private static (ushort Start, ushort End) Range(ReadOnlySpan<byte> body) =>
        (ReadUInt16(body), ReadUInt16(body[2..]));

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt16LittleEndian(bytes);

    private static byte[] UuidBytes(BleUuid uuid) => uuid.ToByteArray();

    private ushort Add(ushort type, byte[] value)
    {
        var attributeHandle = (ushort)(_attributes.Count + 1);
        _attributes.Add(new Attribute(attributeHandle, type, value));
        return attributeHandle;
    }

    /// <summary> Builds the characteristics of a service </summary>
    public sealed class ServiceBuilder(FakePeripheral peripheral)
    {
        private readonly FakePeripheral _peripheral = peripheral;

        /// <summary> Adds a characteristic with a 16-bit UUID; one with notify or indicate gets a CCCD </summary>
        /// <returns> The handle of its value </returns>
        public ushort AddCharacteristic(ushort uuid, GattProperty properties, byte[]? value = null)
        {
            var valueHandle = (ushort)(_peripheral._attributes.Count + 2);
            byte[] declaration =
            [
                (byte)properties,
                (byte)valueHandle,
                (byte)(valueHandle >> 8),
                (byte)uuid,
                (byte)(uuid >> 8),
            ];
            _peripheral.Add(0x2803, declaration);
            _peripheral.Add(uuid, value ?? []);
            if ((properties & (GattProperty.Notify | GattProperty.Indicate)) != 0)
                _peripheral.Add(0x2902, [0x00, 0x00]);
            return valueHandle;
        }

        /// <summary> Adds a descriptor to the characteristic added last </summary>
        /// <returns> Its handle </returns>
        public ushort AddDescriptor(ushort uuid, byte[] value) => _peripheral.Add(uuid, value);
    }

    private sealed class Attribute(ushort handle, ushort type, byte[] value)
    {
        public ushort Handle { get; } = handle;
        public ushort Type { get; } = type;
        public byte[] Value { get; set; } = value;
        public ushort GroupEnd { get; set; }
    }
}
