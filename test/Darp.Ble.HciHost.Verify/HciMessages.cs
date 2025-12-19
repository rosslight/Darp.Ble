using System.Buffers.Binary;
using System.Diagnostics;
using Darp.BinaryObjects;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using UInt48 = Darp.Ble.Hci.Payload.Command.UInt48;

namespace Darp.Ble.HciHost.Verify;

public static class HciMessages
{
    public static HciMessage HciLeReadNumberOfSupportedAdvertisingSetsEvent(
        HciCommandStatus status = HciCommandStatus.Success,
        byte numSupportedAdvertisingSets = 1
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_Read_Number_Of_Supported_Advertising_Sets,
            new HciLeReadNumberOfSupportedAdvertisingSetsResult
            {
                Status = status,
                NumSupportedAdvertisingSets = numSupportedAdvertisingSets,
            }
        );

    public static HciMessage HciLeSetExtendedAdvertisingParametersEvent(
        HciCommandStatus status = HciCommandStatus.Success,
        sbyte selectedTxPower = 0
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_PARAMETERS_V1,
            new HciLeSetExtendedAdvertisingParametersResult { Status = status, SelectedTxPower = selectedTxPower }
        );

    public static HciMessage HciLeSetAdvertisingSetRandomAddressEvent(
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_ADVERTISING_SET_RANDOM_ADDRESS,
            new HciLeSetAdvertisingSetRandomAddressResult { Status = status }
        );

    public static HciMessage HciLeSetExtendedAdvertisingEnableEvent(
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_ENABLE,
            new HciLeSetExtendedAdvertisingEnableResult { Status = status }
        );

    public static HciMessage HciLeRemoveAdvertisingSetEvent(HciCommandStatus status = HciCommandStatus.Success) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_Remove_Advertising_Set,
            new HciLeRemoveAdvertisingSetResult { Status = status }
        );

    public static HciMessage HciLeExtendedCreateConnectionCommandStatusEvent(
        HciCommandStatus status = HciCommandStatus.Success,
        byte numHciCommandPackets = 1
    ) =>
        HciMessage.EventToHost(
            new HciCommandStatusEvent
            {
                Status = status,
                NumHciCommandPackets = numHciCommandPackets,
                CommandOpCode = HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            }
        );

    public static HciMessage HciLeEnhancedConnectionCompleteEvent(
        ushort connectionHandle,
        HciLeConnectionRole role,
        byte peerAddressType,
        ulong peerAddress,
        ushort connectionInterval,
        ushort peripheralLatency,
        ushort supervisionTimeout,
        byte centralClockAccuracy = 0,
        HciCommandStatus status = HciCommandStatus.Success
    )
    {
        var evt = new HciLeEnhancedConnectionCompleteV1Event
        {
            SubEventCode = HciLeEnhancedConnectionCompleteV1Event.SubEventType,
            Status = status,
            ConnectionHandle = connectionHandle,
            Role = role,
            PeerAddressType = peerAddressType,
            PeerAddress = peerAddress,
            LocalResolvablePrivateAddress = default,
            PeerResolvablePrivateAddress = default,
            ConnectionInterval = connectionInterval,
            PeripheralLatency = peripheralLatency,
            SupervisionTimeout = supervisionTimeout,
            CentralClockAccuracy = centralClockAccuracy,
        };
        return HciMessage.LeEventToHost(evt);
    }

    public static HciMessage HciLeReadPhyEvent(
        ushort connectionHandle,
        byte txPhy,
        byte rxPhy,
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_READ_PHY,
            new HciLeReadPhyResult
            {
                Status = status,
                ConnectionHandle = connectionHandle,
                TxPhy = txPhy,
                RxPhy = rxPhy,
            }
        );

    public static HciMessage HciDisconnectionCompleteEvent(
        ushort connectionHandle,
        HciCommandStatus reason = HciCommandStatus.RemoteUserTerminatedConnection,
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.EventToHost(
            new HciDisconnectionCompleteEvent
            {
                Status = status,
                ConnectionHandle = connectionHandle,
                Reason = reason,
            }
        );

    public static HciMessage AttToHost<TAttPdu>(ushort connectionHandle, TAttPdu attPdu)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        byte[] attBytes = attPdu.ToArrayLittleEndian();
        var l2CapPayload = new byte[4 + attBytes.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(l2CapPayload, (ushort)attBytes.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(l2CapPayload.AsSpan(2), 0x0004);
        attBytes.CopyTo(l2CapPayload.AsSpan(4));

        var aclPacket = new HciAclPacket(
            connectionHandle,
            PacketBoundaryFlag.FirstAutoFlushable,
            BroadcastFlag.PointToPoint,
            (ushort)l2CapPayload.Length,
            l2CapPayload
        );

        return HciMessage.AclToHost(aclPacket.ToArrayLittleEndian());
    }

    public static HciMessage AttExchangeMtuResponse(ushort connectionHandle, ushort serverRxMtu)
    {
        return AttToHost(connectionHandle, new AttExchangeMtuRsp { ServerRxMtu = serverRxMtu });
    }

    public static HciMessage AttReadByGroupTypeResponse(
        ushort connectionHandle,
        params AttGroupTypeData[] attributeDataList
    )
    {
        byte length = attributeDataList.Length > 0 ? checked((byte)attributeDataList[0].GetByteCount()) : (byte)0;
        Debug.Assert(attributeDataList.All(x => x.GetByteCount() == length));

        return AttToHost(
            connectionHandle,
            new AttReadByGroupTypeRsp { Length = length, AttributeDataList = attributeDataList }
        );
    }

    public static HciMessage AttReadByTypeResponse(
        ushort connectionHandle,
        params AttReadByTypeData[] attributeDataList
    )
    {
        if (attributeDataList.Length == 0)
        {
            return AttToHost(connectionHandle, new AttReadByTypeRsp { Length = 0, AttributeDataList = [] });
        }
        // Length includes handle (2 bytes) + value length
        byte valueLength = checked((byte)attributeDataList[0].Value.Length);
        Debug.Assert(attributeDataList.All(x => x.Value.Length == valueLength));
        byte length = checked((byte)(2 + valueLength));

        return AttToHost(
            connectionHandle,
            new AttReadByTypeRsp { Length = length, AttributeDataList = attributeDataList }
        );
    }

    public static HciMessage AttNotFoundErrorResponse(
        ushort connectionHandle,
        AttOpCode requestOpCode,
        ushort handle
    ) => AttErrorResponse(connectionHandle, requestOpCode, handle, AttErrorCode.AttributeNotFoundError);

    public static HciMessage AttFindInformationResponse(
        ushort connectionHandle,
        params AttFindInformationData[] informationDataList
    )
    {
        if (informationDataList.Length == 0)
        {
            return AttToHost(
                connectionHandle,
                new AttFindInformationRsp
                {
                    Format = AttFindInformationFormat.HandleAnd16BitUuid,
                    InformationData = informationDataList,
                }
            );
        }
        // Determine format based on first UUID length
        int uuidLength = informationDataList[0].Uuid.Length;
        Debug.Assert(informationDataList.All(x => x.Uuid.Length == uuidLength));
        AttFindInformationFormat format =
            uuidLength == 2
                ? AttFindInformationFormat.HandleAnd16BitUuid
                : AttFindInformationFormat.HandleAnd128BitUuid;

        return AttToHost(
            connectionHandle,
            new AttFindInformationRsp { Format = format, InformationData = informationDataList }
        );
    }

    public static HciMessage AttErrorResponse(
        ushort connectionHandle,
        AttOpCode requestOpCode,
        ushort handle,
        AttErrorCode errorCode
    ) =>
        AttToHost(
            connectionHandle,
            new AttErrorRsp
            {
                RequestOpCode = requestOpCode,
                Handle = handle,
                ErrorCode = errorCode,
            }
        );

    public static HciMessage HciNumberOfCompletedPacketsEvent(ushort connectionHandle, ushort numCompletedPackets = 1)
    {
        return HciMessage.EventToHost(
            new HciNumberOfCompletedPacketsEvent
            {
                Handles =
                [
                    new HciNumberOfCompletedPackets
                    {
                        ConnectionHandle = connectionHandle,
                        NumCompletedPackets = numCompletedPackets,
                    },
                ],
                NumHandles = 1,
            }
        );
    }
}
