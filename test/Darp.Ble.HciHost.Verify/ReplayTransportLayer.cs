using System.Collections.Concurrent;
using System.Globalization;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Verify;

/*
public sealed class ReplayTransportBuilder
{
    public ReplayTransportLayer RespondWith(Func<HciMessage, (HciMessage?, TimeSpan)> onResponse) { }

    public ReplayTransportLayer RespondWithEvent(HciEventCode eventCode, byte[] resultBytes, TimeSpan? delay = null) { }

    public ReplayTransportLayer RespondWithCommandCompleteEvent(string resultHexString, TimeSpan? delay = null)
    {
        byte[] resultBytes = Convert.FromHexString(resultHexString);
        return RespondWithEvent(HciEventCode.HCI_Command_Complete, resultBytes, delay);
    }
}
*/

/// <summary> A replay transport layer </summary>
/// <param name="responseBuilder"> A callback to build response messages </param>
/// <param name="messagesToSkip"> The number of messages to skip when a recording is requested </param>
/// <param name="logger"> An optional logger for debugging </param>
public sealed class ReplayTransportLayer(
    Func<HciMessage, int, (HciMessage? Message, TimeSpan Delay)?> responseBuilder,
    int messagesToSkip,
    ILogger<ReplayTransportLayer>? logger
) : ITransportLayer
{
    private readonly Func<HciMessage, int, (HciMessage? Message, TimeSpan Delay)?> _responseBuilder = responseBuilder;
    private readonly int _messagesToSkip = messagesToSkip;
    private readonly ILogger<ReplayTransportLayer>? _logger = logger;
    private readonly ConcurrentQueue<HciMessage> _messagesToController = [];
    private readonly ConcurrentQueue<HciMessage> _messagesToHost = [];
    private Action<HciPacket>? _onReceived;

    public static readonly HciMessage[] InitializeHciDeviceMessages =
    [
        // HCI_Reset
        HciMessage.CommandCompleteEventToHost("01030C00"),
        // HCI_READ_LOCAL_SUPPORTED_COMMANDS
        HciMessage.CommandCompleteEventToHost(
            "010210002000800000C000000000E40000002822000000000000040000F7FFFF7F00000030C07FFEFFE38007000400000040000000000000000000000000000000000000"
        ),
        // HCI_LE_READ_LOCAL_SUPPORTED_FEATURES
        HciMessage.CommandCompleteEventToHost("01032000FD59000000000000"),
        // HCI_Read_Local_Version_Information
        HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
        // HCI_READ_LOCAL_SUPPORTED_FEATURES
        HciMessage.CommandCompleteEventToHost("010310000000000060000000"),
        // HCI_Set_Event_Mask
        HciMessage.CommandCompleteEventToHost("01010C00"),
        // HCI_LE_Set_Event_Mask
        HciMessage.CommandCompleteEventToHost("01012000"),
        // HCI_LE_Read_Buffer_Size_V1
        HciMessage.CommandCompleteEventToHost("01022000FB0003"),
        // HCI_LE_Read_Suggested_Default_Data_Length
        HciMessage.CommandCompleteEventToHost("012320001B004801"),
        // HCI_LE_WRITE_SUGGESTED_DEFAULT_DATA_LENGTH
        HciMessage.CommandCompleteEventToHost("01242000"),
    ];

    public static readonly HciMessage[] InitializeBleDeviceMessages = InitializeHciDeviceMessages
        .Concat(
            [
                // HCI_LE_Set_Random_Address
                HciMessage.CommandCompleteEventToHost("01052000"),
                // HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH
                HciMessage.CommandCompleteEventToHost("013A20003E00"),
            ]
        )
        .ToArray();

    /// <summary> Provides all messages sent to the controller. Skips the defined number of messages </summary>
    public IReadOnlyCollection<HciMessage> MessagesToController =>
        _messagesToController.Skip(_messagesToSkip).ToArray();

    /// <summary> Provides all messages sent to the host by the replay transport layer </summary>
    public IReadOnlyCollection<HciMessage> MessagesToHost => _messagesToHost.Skip(_messagesToSkip).ToArray();

    /// <summary> Initializes a default replay transport layer with no messages </summary>
    /// <param name="messagesToSkip"> The number of messages to skip when a recording is requested </param>
    /// <param name="logger"> An optional logger for debugging </param>
    public ReplayTransportLayer(int messagesToSkip = 0, ILogger<ReplayTransportLayer>? logger = null)
        : this((_, _) => (HciMessage.None, TimeSpan.Zero), messagesToSkip, logger) { }

    private ReplayTransportLayer(
        IReadOnlyList<HciMessage?> messages,
        int messagesToSkip = 0,
        ILogger<ReplayTransportLayer>? logger = null
    )
        : this((_, i) => IterateHciMessages(messages, i), messagesToSkip, logger) { }

    private static (HciMessage?, TimeSpan) IterateHciMessages(IReadOnlyList<HciMessage?> messages, int i)
    {
        if (messages.Count <= i)
        {
            throw new ArgumentOutOfRangeException(
                nameof(messages),
                string.Create(CultureInfo.InvariantCulture, $"Not enough responses provided. Replayed {i} messages")
            );
        }

        return (messages[i], TimeSpan.Zero);
    }

    public void Push(HciMessage message)
    {
        _onReceived?.Invoke(new HciPacket(message.Type, message.PduBytes));
    }

    void ITransportLayer.Enqueue(IHciPacket packet)
    {
        byte[] bytes = packet.ToArrayLittleEndian();
        var message = new HciMessage(HciDirection.HostToController, packet.PacketType, bytes);
        _messagesToController.Enqueue(message);
        _logger?.LogDebug("ReplayTransportLayer: Packet from Host: {PacketBytes}", Convert.ToHexString(bytes));

        (HciMessage? Message, TimeSpan Delay)? response = _responseBuilder(message, _messagesToController.Count - 1);
        if (response?.Message is null)
            return;
        HciMessage responseMessage = response.Value.Message;

        _ = Task.Run(async () =>
        {
            await Task.Delay(response.Value.Delay);

            _logger?.LogDebug(
                "ReplayTransportLayer: Packet to Host: {PacketBytes}",
                Convert.ToHexString(responseMessage.PduBytes)
            );
            _messagesToHost.Enqueue(responseMessage);
            _onReceived?.Invoke(new HciPacket(responseMessage.Type, responseMessage.PduBytes));
        });
    }

    ValueTask ITransportLayer.InitializeAsync(Action<HciPacket> onReceived, CancellationToken cancellationToken)
    {
        _onReceived = onReceived;
        _logger?.LogDebug("ReplayTransportLayer: Initialized");
        return ValueTask.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    /// <summary> Creates a transport layer replaying the provided <paramref name="messages"/> </summary>
    /// <param name="messages"> The messages to be replayed </param>
    /// <returns> The <see cref="ReplayTransportLayer"/> </returns>
    /// <param name="logger"> An optional logger for debugging </param>
    public static ReplayTransportLayer Replay(
        IEnumerable<HciMessage> messages,
        ILogger<ReplayTransportLayer>? logger = null
    )
    {
        return new ReplayTransportLayer(messages.ToArray(), logger: logger);
    }

    /// <summary> Creates a transport layer replaying the provided <paramref name="messages"/> after the device was initialized </summary>
    /// <param name="messages"> The messages to be replayed </param>
    /// <returns> The <see cref="ReplayTransportLayer"/> </returns>
    /// <param name="logger"> An optional logger for debugging </param>
    public static ReplayTransportLayer ReplayAfterInitialization(
        IEnumerable<HciMessage> messages,
        ILogger<ReplayTransportLayer>? logger = null
    )
    {
        return new ReplayTransportLayer(
            InitializeHciDeviceMessages.Concat(messages).ToArray(),
            InitializeHciDeviceMessages.Length,
            logger
        );
    }

    /// <summary> Creates a transport layer replaying the provided <paramref name="messages"/> after the device was initialized </summary>
    /// <param name="messages"> The messages to be replayed </param>
    /// <returns> The <see cref="ReplayTransportLayer"/> </returns>
    /// <param name="logger"> An optional logger for debugging </param>
    public static ReplayTransportLayer ReplayAfterBleDeviceInitialization(
        IEnumerable<HciMessage> messages,
        ILogger<ReplayTransportLayer>? logger = null
    )
    {
        return new ReplayTransportLayer(
            InitializeBleDeviceMessages.Concat(messages).ToArray(),
            InitializeBleDeviceMessages.Length,
            logger
        );
    }
}
