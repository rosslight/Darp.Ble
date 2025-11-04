using System.Buffers.Binary;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary> An l2Cap assembler </summary>
public interface IL2CapAssembler : IMessageSource
{
    void OnAclPacket(HciAclPacket packet);
}

/// <summary> Assemble L2Cap packages from AclPackages </summary>
/// <param name="connectionHandle"> The connection to filter for </param>
/// <param name="logger"> The logger to use for logging </param>
[MessageSource]
public sealed partial class L2CapAssembler(ushort connectionHandle, ILogger<L2CapAssembler>? logger) : IL2CapAssembler
{
    private readonly ushort _connectionHandle = connectionHandle;
    private readonly ILogger<L2CapAssembler>? _logger = logger;
    private readonly List<byte> _dataBytes = [];
    private ushort _targetLength;

    public void OnAclPacket(HciAclPacket packet)
    {
        if (packet.ConnectionHandle != _connectionHandle)
            return;
        if (packet.PacketBoundaryFlag is PacketBoundaryFlag.FirstAutoFlushable)
        {
            if (_dataBytes.Count > 0)
            {
                _logger?.LogWarning(
                    "Got packet {@Packet} but collector still has an open entry: {@Collector}",
                    packet,
                    _dataBytes
                );
                return;
            }
            if (packet.DataBytes.Length < 4)
            {
                _logger?.LogWarning("Got packet {@Packet} but data bytes are too short for header", packet);
                return;
            }
            _targetLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.DataBytes.Span);
            _dataBytes.AddRange(packet.DataBytes.Span);
        }
        else if (packet.PacketBoundaryFlag is PacketBoundaryFlag.ContinuingFragment)
        {
            if (_targetLength == 0 || _dataBytes.Count == 0)
            {
                _logger?.LogWarning(
                    "Got packet {@Packet} but collector is invalid: {@Collector} / {TargetLength}",
                    packet,
                    _dataBytes,
                    _targetLength
                );
                return;
            }
            _dataBytes.AddRange(packet.DataBytes.Span);
        }
        else
        {
            _logger?.LogWarning("Got unsupported packet boundary flag for packet {@Packet}", packet);
            return;
        }

        if (_dataBytes.Count > _targetLength + 4)
        {
            _logger?.LogWarning("Got too many bytes in {@List} after packet {@Packet}", _dataBytes, packet);
            return;
        }
        if (_dataBytes.Count != _targetLength + 4)
            return;
        Span<byte> assembledPdu = _dataBytes.ToArray().AsSpan();
        ushort channelId = BinaryPrimitives.ReadUInt16LittleEndian(assembledPdu[2..4]);
        PublishL2CapPdu(new L2CapPdu(channelId, assembledPdu[4..]));
        _dataBytes.Clear();
    }

    private void PublishL2CapPdu(L2CapPdu l2Cap)
    {
        if (l2Cap.ChannelId is not 0x04 || l2Cap.Pdu.Length < 1)
        {
            PublishMessage(l2Cap);
            return;
        }

        var opCode = (AttOpCode)l2Cap.Pdu[0];
        switch (opCode)
        {
            case AttOpCode.ATT_ERROR_RSP when AttErrorRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_EXCHANGE_MTU_REQ when AttExchangeMtuReq.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_EXCHANGE_MTU_RSP when AttExchangeMtuRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_FIND_INFORMATION_REQ
                when AttFindInformationReq.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_FIND_INFORMATION_RSP
                when AttFindInformationRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_FIND_BY_TYPE_VALUE_REQ
                when AttFindByTypeValueReq.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP
                when AttFindByTypeValueRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_BY_TYPE_REQ
                when AttReadByTypeReq<ushort>.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_BY_TYPE_RSP when AttReadByTypeRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_REQ when AttReadReq.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_RSP when AttReadRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ
                when AttReadByGroupTypeReq<ushort>.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP
                when AttReadByGroupTypeRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_WRITE_REQ when AttWriteReq.TryReadLittleEndian(l2Cap.Pdu, out var req):
                PublishEventPacketAndLog(req, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_WRITE_RSP when AttWriteRsp.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_WRITE_CMD when AttWriteCmd.TryReadLittleEndian(l2Cap.Pdu, out var cmd):
                PublishEventPacketAndLog(cmd, l2Cap.Pdu);
                break;
            case AttOpCode.ATT_HANDLE_VALUE_NTF when AttHandleValueNtf.TryReadLittleEndian(l2Cap.Pdu, out var rsp):
                PublishEventPacketAndLog(rsp, l2Cap.Pdu);
                break;
            default:
                using (_logger?.ForContext("PacketPayload", l2Cap.Pdu.ToArray()))
                {
                    _logger?.LogReceivedUnknownAttPacket("Controller", "Host", _connectionHandle, opCode);
                }
                PublishMessage(l2Cap);
                break;
        }
    }

    private void PublishEventPacketAndLog<T>(T packet, ReadOnlySpan<byte> packetPayloadBytes, bool shouldLog = true)
        where T : IAttPdu
    {
        if (shouldLog)
        {
            LogPacketControllerToHost(packet, packetPayloadBytes, $"{T.ExpectedOpCode.ToString().ToUpperInvariant()}");
        }
        PublishMessage(packet);
    }

    private void LogPacketControllerToHost<T>(T packet, ReadOnlySpan<byte> packetPayloadBytes, string packetName)
    {
        using (
            _logger?.BeginScope(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["@Packet"] = packet,
                    ["PacketPayload"] = packetPayloadBytes.ToArray(),
                }
            )
        )
        {
            _logger?.LogAttPacketTransmission("Controller", "Host", _connectionHandle, packetName);
        }
    }
}
