using System.Buffers.Binary;
using System.Threading.Channels;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost.Verify;

/// <summary>
/// A controller for tests: answers commands by their opcode, records the traffic in order, and delivers
/// everything to the host on its own loop, like a transport's read thread
/// </summary>
public sealed class FakeController : ITransportLayer
{
    private readonly Lock _lock = new();
    private readonly Dictionary<HciOpCode, Func<HciMessage, IEnumerable<HciMessage>>> _answers = [];
    private readonly List<HciMessage> _messages = [];
    private readonly Channel<(HciMessage Message, TaskCompletionSource Delivered)> _toHost = Channel.CreateUnbounded<(
        HciMessage,
        TaskCompletionSource
    )>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Channel<HciMessage> _aclToController = Channel.CreateUnbounded<HciMessage>();
    private Action<HciPacket>? _onReceived;
    private Action<Exception>? _onError;
    private Task? _loop;
    private int _recordFrom;
    private TaskCompletionSource _sent = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary> Creates a controller that answers the host's setup and the commands without a connection </summary>
    public FakeController()
    {
        foreach ((HciOpCode opCode, string parameters) in DefaultCommandCompletes)
        {
            HciMessage answer = HciMessage.CommandCompleteEventToHost(parameters);
            _answers[opCode] = _ => [answer];
        }
        Answer(
            HciOpCode.HCI_Disconnect,
            command => [CommandStatus(HciOpCode.HCI_Disconnect), DisconnectionComplete(command)]
        );
        Answer(
            HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            _ => [CommandStatus(HciOpCode.HCI_LE_Extended_Create_Connection_V1)]
        );
    }

    /// <summary> The answers to the host's setup, by opcode </summary>
    public static IReadOnlyList<(HciOpCode OpCode, string Parameters)> DefaultCommandCompletes { get; } =
    [
        (HciOpCode.HCI_Reset, "01030C00"),
        (
            HciOpCode.HCI_Read_Local_Supported_Commands,
            "010210002000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFEFFE38007000400000040000000000000000000000000000000000000"
        ),
        (HciOpCode.HCI_LE_Read_Local_Supported_Features, "01032000FD59000000000000"),
        (HciOpCode.HCI_Read_Local_Version_Information, "010110000D02110D59000211"),
        (HciOpCode.HCI_Read_Local_Supported_Features, "010310000000000060000000"),
        (HciOpCode.HCI_Set_Event_Mask, "01010C00"),
        (HciOpCode.HCI_LE_Set_Event_Mask, "01012000"),
        // 251 bytes per ACL packet, 3 packets in the controller
        (HciOpCode.HCI_LE_Read_Buffer_Size_V1, "01022000FB0003"),
        (HciOpCode.HCI_LE_Read_Suggested_Default_Data_Length, "012320001B004801"),
        (HciOpCode.HCI_LE_Write_Suggested_Default_Data_Length, "01242000"),
        (HciOpCode.HCI_LE_Set_Random_Address, "01052000"),
        (HciOpCode.HCI_LE_Set_Extended_Scan_Parameters, "01412000"),
        (HciOpCode.HCI_LE_Set_Extended_Scan_Enable, "01422000"),
        // 251 bytes of advertising data, 4 advertising sets
        (HciOpCode.HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH, "013A2000FB00"),
        (HciOpCode.HCI_LE_Read_Number_Of_Supported_Advertising_Sets, "013B200004"),
        (HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_PARAMETERS_V1, "0136200000"),
        (HciOpCode.HCI_LE_SET_ADVERTISING_SET_RANDOM_ADDRESS, "01352000"),
        (HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_DATA, "01372000"),
        (HciOpCode.HCI_LE_Set_Extended_Scan_Response_Data, "01382000"),
        (HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_ENABLE, "01392000"),
        (HciOpCode.HCI_LE_Remove_Advertising_Set, "013C2000"),
    ];

    /// <summary> Whether every ACL packet from the host is reported back as completed at once; turn off to hold the controller's buffers </summary>
    public bool CompletePacketsAutomatically { get; set; } = true;

    /// <summary> The traffic since the last <see cref="StartRecording"/>, in order </summary>
    public IReadOnlyList<HciMessage> Messages
    {
        get
        {
            lock (_lock)
                return _messages.GetRange(_recordFrom, _messages.Count - _recordFrom);
        }
    }

    /// <summary> The commands the host sent since the last <see cref="StartRecording"/> </summary>
    public IReadOnlyList<HciOpCode> Commands =>
        [
            .. Messages
                .Where(message =>
                    message is { Direction: HciDirection.HostToController, Type: HciPacketType.HciCommand }
                )
                .Select(OpCodeOf),
        ];

    /// <summary> Forgets the traffic so far, e.g. the setup </summary>
    public void StartRecording()
    {
        lock (_lock)
            _recordFrom = _messages.Count;
    }

    /// <summary> Answers a command with the given messages; replaces the earlier answer </summary>
    public void Answer(HciOpCode opCode, Func<HciMessage, IEnumerable<HciMessage>> answer)
    {
        lock (_lock)
            _answers[opCode] = answer;
    }

    /// <summary> Answers a command with a command complete event of the given parameters, e.g. "01022000FB0003" </summary>
    public void AnswerCommandComplete(HciOpCode opCode, string parameters)
    {
        HciMessage answer = HciMessage.CommandCompleteEventToHost(parameters);
        Answer(opCode, _ => [answer]);
    }

    /// <summary> Waits until the host sent the command for the given time since the last <see cref="StartRecording"/> </summary>
    /// <returns> The command; its answer reaches the host before anything pushed afterwards </returns>
    public async Task<HciMessage> WaitForCommandAsync(HciOpCode opCode, int occurrence = 1)
    {
        while (true)
        {
            Task sent;
            lock (_lock)
            {
                HciMessage? command = _messages
                    .Skip(_recordFrom)
                    .Where(message => message.Type is HciPacketType.HciCommand && OpCodeOf(message) == opCode)
                    .Skip(occurrence - 1)
                    .FirstOrDefault();
                if (command is not null)
                    return command;
                sent = _sent.Task;
            }
            await sent.ConfigureAwait(false);
        }
    }

    /// <summary> Leaves a command unanswered </summary>
    public void Hold(HciOpCode opCode) => Answer(opCode, _ => []);

    /// <summary> Sends a message to the host </summary>
    /// <returns> A task that completes once the host handled it </returns>
    public Task PushAsync(HciMessage message)
    {
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _toHost.Writer.TryWrite((message, delivered));
        return delivered.Task;
    }

    /// <summary> Reports ACL packets of a link as sent, which frees the controller's buffers </summary>
    public Task CompletePacketsAsync(ushort connectionHandle, ushort count = 1) =>
        PushAsync(HciMessages.HciNumberOfCompletedPacketsEvent(connectionHandle, count));

    /// <summary> The ACL packets the host sent since the last <see cref="StartRecording"/>, by link, in order </summary>
    public IReadOnlyList<(ushort Handle, byte[] Packet)> AclPackets =>
        [
            .. Messages
                .Where(message =>
                    message is { Direction: HciDirection.HostToController, Type: HciPacketType.HciAclData }
                )
                .Select(message =>
                    ((ushort)(BinaryPrimitives.ReadUInt16LittleEndian(message.PduBytes) & 0x0FFF), message.PduBytes)
                ),
        ];

    /// <summary> The next ACL packet the host sent </summary>
    public ValueTask<HciMessage> ReadAclAsync(CancellationToken cancellationToken) =>
        _aclToController.Reader.ReadAsync(cancellationToken);

    /// <summary> Reports a failure of the transport to the host </summary>
    public void Fail(Exception exception) => _onError?.Invoke(exception);

    /// <summary> A command status event for the given command </summary>
    public static HciMessage CommandStatus(HciOpCode opCode, HciCommandStatus status = HciCommandStatus.Success) =>
        HciMessage.EventToHost(
            new HciCommandStatusEvent
            {
                Status = status,
                NumHciCommandPackets = 1,
                CommandOpCode = opCode,
            }
        );

    private static HciOpCode OpCodeOf(HciMessage command) =>
        (HciOpCode)BinaryPrimitives.ReadUInt16LittleEndian(command.PduBytes);

    private static HciMessage DisconnectionComplete(HciMessage disconnect)
    {
        // Opcode (2), length (1), handle (2), reason (1)
        ushort handle = BinaryPrimitives.ReadUInt16LittleEndian(disconnect.PduBytes.AsSpan(3));
        return HciMessages.HciDisconnectionCompleteEvent(handle, HciCommandStatus.ConnectionTerminatedByLocalHost);
    }

    void ITransportLayer.Enqueue(IHciPacket packet)
    {
        // The host may reuse the packet's buffers once Enqueue returned, so the bytes are copied here
        var message = new HciMessage(HciDirection.HostToController, packet.PacketType, packet.ToArrayLittleEndian());
        Func<HciMessage, IEnumerable<HciMessage>>? answer = null;
        TaskCompletionSource sent;
        lock (_lock)
        {
            _messages.Add(message);
            sent = _sent;
            _sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (message.Type is HciPacketType.HciCommand && !_answers.TryGetValue(OpCodeOf(message), out answer))
            {
                sent.TrySetResult();
                throw new InvalidOperationException($"The fake controller has no answer to {OpCodeOf(message)}");
            }
        }
        if (answer is not null)
        {
            try
            {
                foreach (HciMessage response in answer(message))
                    _ = PushAsync(response);
            }
            finally
            {
                // Waiters see the command only once its answer is queued for the host
                sent.TrySetResult();
            }
            return;
        }
        sent.TrySetResult();
        if (message.Type is not HciPacketType.HciAclData)
            return;
        // Completed before the test can read the packet, so an answer the test pushes comes after it
        if (CompletePacketsAutomatically)
        {
            ushort handle = (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(message.PduBytes) & 0x0FFF);
            _ = PushAsync(HciMessages.HciNumberOfCompletedPacketsEvent(handle));
        }
        _aclToController.Writer.TryWrite(message);
    }

    ValueTask ITransportLayer.InitializeAsync(
        Action<HciPacket> onReceived,
        Action<Exception> onError,
        CancellationToken cancellationToken
    )
    {
        _onReceived = onReceived;
        _onError = onError;
        _loop = Task.Run(DeliverAsync, CancellationToken.None);
        return ValueTask.CompletedTask;
    }

    private async Task DeliverAsync()
    {
        await foreach ((HciMessage message, TaskCompletionSource delivered) in _toHost.Reader.ReadAllAsync())
        {
            lock (_lock)
                _messages.Add(message);
            try
            {
                _onReceived?.Invoke(new HciPacket(message.Type, message.PduBytes));
                delivered.TrySetResult();
            }
            catch (Exception e)
            {
                delivered.TrySetException(e);
            }
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        _toHost.Writer.TryComplete();
        _aclToController.Writer.TryComplete();
        if (_loop is not null)
            await _loop.ConfigureAwait(false);
    }
}
