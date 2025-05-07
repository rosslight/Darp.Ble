using System.Collections.Concurrent;
using System.Globalization;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost.Verify;

/// <summary> A replay transport layer </summary>
/// <param name="responseBuilder"> A callback to build response messages </param>
/// <param name="messagesToSkip"> The number of messages to skip when a recording is requested </param>
public sealed class ReplayTransportLayer(
    Func<HciMessage, int, (HciMessage? Message, TimeSpan Delay)?> responseBuilder,
    int messagesToSkip
) : ITransportLayer
{
    private readonly Func<HciMessage, int, (HciMessage? Message, TimeSpan Delay)?> _responseBuilder = responseBuilder;
    private readonly int _messagesToSkip = messagesToSkip;
    private readonly ConcurrentQueue<HciMessage> _messagesToController = [];
    private readonly ConcurrentQueue<HciMessage> _messagesToHost = [];
    private Action<HciPacket>? _onReceived;

    /// <summary> Provides all messages sent to the controller. Skips the defined amount of messages </summary>
    public IReadOnlyCollection<HciMessage> MessagesToController =>
        _messagesToController.Skip(_messagesToSkip).ToArray();

    /// <summary> Provides all messages sent to the host by the replay transport layer </summary>
    public IReadOnlyCollection<HciMessage> MessagesToHost => _messagesToHost;

    /// <summary> Initializes a default replay transport layer with no messages </summary>
    /// <param name="messagesToSkip"> The number of messages to skip when a recording is requested </param>
    public ReplayTransportLayer(int messagesToSkip = 0)
        : this((_, _) => (HciMessage.None, TimeSpan.Zero), messagesToSkip) { }

    private ReplayTransportLayer(IReadOnlyList<HciMessage?> messages, int messagesToSkip = 0)
        : this((_, i) => IterateHciMessages(messages, i), messagesToSkip) { }

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

    void ITransportLayer.Enqueue(IHciPacket packet)
    {
        byte[] bytes = packet.ToArrayLittleEndian();
        var message = new HciMessage(HciDirection.HostToController, packet.PacketType, bytes);
        _messagesToController.Enqueue(message);

        (HciMessage? Message, TimeSpan Delay)? response = _responseBuilder(message, _messagesToController.Count - 1);
        if (response?.Message is null)
            return;
        HciMessage responseMessage = response.Value.Message;

        _ = Task.Run(async () =>
        {
            await Task.Delay(response.Value.Delay);

            _messagesToHost.Enqueue(responseMessage);
            _onReceived?.Invoke(new HciPacket(responseMessage.Type, responseMessage.PduBytes));
        });
    }

    ValueTask ITransportLayer.InitializeAsync(Action<HciPacket> onReceived, CancellationToken cancellationToken)
    {
        _onReceived = onReceived;
        return ValueTask.CompletedTask;
    }

    void IDisposable.Dispose() { }

    /// <summary> Creates a transport layer replaying the provided <paramref name="messages"/> </summary>
    /// <param name="messages"> The messages to be replayed </param>
    /// <returns> The <see cref="ReplayTransportLayer"/> </returns>
    public static ReplayTransportLayer Replay(params IEnumerable<HciMessage> messages)
    {
        return new ReplayTransportLayer(messages.ToArray());
    }

    /// <summary> Creates a transport layer replaying the provided <paramref name="messages"/> after the device was initialized </summary>
    /// <param name="messages"> The messages to be replayed </param>
    /// <returns> The <see cref="ReplayTransportLayer"/> </returns>
    public static ReplayTransportLayer ReplayAfterInitialization(params IEnumerable<HciMessage> messages)
    {
        HciMessage[] initializeMessages =
        [
            // HCI_Reset
            HciMessage.CommandCompleteEventToHost("01030C00"),
            // HCI_Read_Local_Version_Information
            HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
            // HCI_Set_Event_Mask
            HciMessage.CommandCompleteEventToHost("01010C00"),
            // HCI_LE_Set_Event_Mask
            HciMessage.CommandCompleteEventToHost("01012000"),
            // HCI_LE_Read_Buffer_Size_V1
            HciMessage.CommandCompleteEventToHost("01022000FB0003"),
            // HCI_LE_Set_Random_Address
            HciMessage.CommandCompleteEventToHost("01052000"),
            // HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH
            HciMessage.CommandCompleteEventToHost("013A20003E00"),
        ];
        return new ReplayTransportLayer(initializeMessages.Concat(messages).ToArray(), initializeMessages.Length);
    }
}
