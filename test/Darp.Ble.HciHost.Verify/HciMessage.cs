using Darp.BinaryObjects;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.HciHost.Verify;

/// <summary> Represents a recorded HCI Communication </summary>
/// <param name="Direction"> The direction the message was sent </param>
/// <param name="Type"> The type of the HCI packet </param>
/// <param name="PduBytes"> The pdu bytes </param>
public sealed record HciMessage(HciDirection Direction, HciPacketType Type, byte[] PduBytes)
{
    /// <summary> Represents a null object. </summary>
    // ReSharper disable once UnassignedReadonlyField
    public static readonly HciMessage? None;

    /// <summary> Constructs a HCI message sent to a host </summary>
    /// <param name="packetType"> The type of the HCI packet </param>
    /// <param name="pduBytes"> The pdu bytes </param>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage ToHost(HciPacketType packetType, byte[] pduBytes) =>
        new(HciDirection.ControllerToHost, packetType, pduBytes);

    /// <summary> Constructs an HCI event message sent to the host </summary>
    /// <param name="hciEvent"> The event that was sent </param>
    /// <typeparam name="TEvent"> The type of the event </typeparam>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage EventToHost<TEvent>(TEvent hciEvent)
        where TEvent : IHciEvent<TEvent>, IBinaryWritable
    {
        byte[] parameterBytes = hciEvent.ToArrayLittleEndian();
        return EventToHost(TEvent.EventCode, parameterBytes);
    }

    /// <summary> Constructs an HCI event message sent to the host </summary>
    /// <param name="eventCode"> The event code of the event sent </param>
    /// <param name="parameterBytes"> The parameter bytes of the event sent </param>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage EventToHost(HciEventCode eventCode, byte[] parameterBytes)
    {
        var evt = new HciPacketEvent
        {
            EventCode = eventCode,
            ParameterTotalLength = (byte)parameterBytes.Length,
            DataBytes = parameterBytes,
        };
        return new HciMessage(HciDirection.ControllerToHost, HciPacketType.HciEvent, evt.ToArrayLittleEndian());
    }

    public static HciMessage LeEventToHost<TEvent>(TEvent hciEvent)
        where TEvent : IHciLeMetaEvent<TEvent>, IBinaryWritable
    {
        return LeEventToHost(hciEvent.ToArrayLittleEndian());
    }

    /// <summary> Constructs an HCI le event message sent to the host </summary>
    /// <param name="parameterBytes"> The parameter bytes of the event sent </param>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage LeEventToHost(byte[] parameterBytes)
    {
        var evt = new HciPacketEvent
        {
            EventCode = HciEventCode.HCI_LE_Meta,
            ParameterTotalLength = (byte)parameterBytes.Length,
            DataBytes = parameterBytes,
        };
        return new HciMessage(HciDirection.ControllerToHost, HciPacketType.HciEvent, evt.ToArrayLittleEndian());
    }

    /// <summary> Constructs an HCI le event message sent to the host </summary>
    /// <param name="parameterHexString"> The hex string of the event parameter bytes </param>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage LeEventToHost(string parameterHexString)
    {
        return LeEventToHost(Convert.FromHexString(parameterHexString));
    }

    /// <summary> Constructs an HCI command complete event message sent to the host </summary>
    /// <param name="opCode"> The OpCode of the command sent </param>
    /// <param name="parameters"> The parameters of the complete event </param>
    /// <param name="numPackets"> The number of packets that are free to sent </param>
    /// <typeparam name="TParameters"> The type of the parameters </typeparam>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage CommandCompleteEventToHost<TParameters>(
        HciOpCode opCode,
        TParameters parameters,
        byte numPackets = 1
    )
        where TParameters : ICommandStatusResult, IBinaryWritable
    {
        return EventToHost(
            new HciCommandCompleteEvent
            {
                NumHciCommandPackets = numPackets,
                CommandOpCode = opCode,
                ReturnParameters = parameters.ToArrayLittleEndian(),
            }
        );
    }

    /// <summary> Constructs an HCI command complete event message sent to the host </summary>
    /// <param name="parameterHexString"> The hex string of the event parameter bytes </param>
    /// <returns> The representation of the HCI message </returns>
    public static HciMessage CommandCompleteEventToHost(string parameterHexString)
    {
        return EventToHost(HciEventCode.HCI_Command_Complete, Convert.FromHexString(parameterHexString));
    }

    /// <summary> Constructs an HCI ACL message sent to the host </summary>
    /// <param name="pduBytes"></param>
    /// <returns></returns>
    public static HciMessage AclToHost(byte[] pduBytes) =>
        new(HciDirection.ControllerToHost, HciPacketType.HciAclData, pduBytes);
}
