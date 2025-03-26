using System.Collections.Concurrent;
using System.IO.Ports;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci.Transport;

/// <summary> The received HCI packet </summary>
/// <param name="packetType"> The type of the HCI packet </param>
/// <param name="pdu"> The pdu </param>
public readonly ref struct HciPacket(HciPacketType packetType, ReadOnlySpan<byte> pdu)
{
    public HciPacketType PacketType { get; } = packetType;
    public ReadOnlySpan<byte> Pdu { get; } = pdu;
}

/// <summary> A transport layer which sends HCI packets via a <see cref="SerialPort"/> </summary>
public sealed class H4TransportLayer : ITransportLayer
{
    private readonly ILogger _logger;
    private readonly SerialPort _serialPort;
    private readonly ConcurrentQueue<IHciPacket> _txQueue;
    private readonly CancellationTokenSource _cancelSource;
    private readonly CancellationToken _cancelToken;
    private bool _isDisposing;

    /// <summary> Instantiate a new h4 transport layer </summary>
    /// <param name="portName"> The name of the serial port </param>
    /// <param name="logger"> An optional logger </param>
    public H4TransportLayer(string portName, ILogger<H4TransportLayer> logger)
    {
        _logger = logger;
        _serialPort = new SerialPort(portName);
        _txQueue = new ConcurrentQueue<IHciPacket>();
        _cancelSource = new CancellationTokenSource();
        _cancelToken = _cancelSource.Token;
    }

    private async ValueTask RunTx()
    {
        try
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                if (!_serialPort.IsOpen || _txQueue.IsEmpty || !_txQueue.TryDequeue(out IHciPacket? packet))
                {
                    await Task.Delay(1, _cancelToken).ConfigureAwait(false);
                    continue;
                }
                var bytes = new byte[1 + packet.GetByteCount()];
                bytes[0] = (byte)packet.PacketType;
                if (!packet.TryWriteLittleEndian(bytes.AsSpan()[1..]))
                {
                    _logger?.LogPacketSendingErrorEncoding(packet);
                    continue;
                }

                await _serialPort.BaseStream.WriteAsync(bytes, _cancelToken).ConfigureAwait(false);
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogTransportDisconnected("Tx");
                return;
            }
            _logger?.LogTransportWithError(e, "Tx", e.Message);
        }
#pragma warning restore CA1031
    }

    private async ValueTask RunRxPacket<TPacket>(
        Memory<byte> buffer,
        byte payloadLengthIndex,
        Action<HciPacket> onReceived
    )
        where TPacket : IHciPacket<TPacket>, IBinaryReadable<TPacket>
    {
        // Read Header
        await _serialPort
            .BaseStream.ReadExactlyAsync(buffer[..TPacket.HeaderLength], _cancelToken)
            .ConfigureAwait(false);
        byte payloadLength = buffer.Span[payloadLengthIndex];
        // Read Payload
        Memory<byte> payloadBuffer = buffer[TPacket.HeaderLength..(TPacket.HeaderLength + payloadLength)];
        await _serialPort.BaseStream.ReadExactlyAsync(payloadBuffer, _cancelToken).ConfigureAwait(false);
        if (
            !TPacket.TryReadLittleEndian(
                buffer[..(TPacket.HeaderLength + payloadLength)].Span,
                out TPacket? packet,
                out _
            )
        )
        {
            _logger.LogPacketReceivingDecodingFailed(
                (byte)TPacket.Type,
                buffer[..(TPacket.HeaderLength + payloadLength)].ToArray(),
                typeof(TPacket).Name
            );
            return;
        }
        onReceived(new HciPacket(TPacket.Type, buffer[..(TPacket.HeaderLength + payloadLength)].Span));
    }

    private async ValueTask RunRx(Action<HciPacket> onReceived)
    {
        // Longest allowed memory
        Memory<byte> buffer = new byte[4 + 255];
        try
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                if (!_serialPort.IsOpen)
                    continue;
                // Read Type
                var type = (HciPacketType)_serialPort.BaseStream.ReadByte();
                switch (type)
                {
                    case HciPacketType.HciEvent:
                        await RunRxPacket<HciEventPacket>(buffer, 1, onReceived).ConfigureAwait(false);
                        break;
                    case HciPacketType.HciAclData:
                        await RunRxPacket<HciAclPacket>(buffer, 2, onReceived).ConfigureAwait(false);
                        break;
                    case HciPacketType.HciCommand:
                    default:
                        _logger?.LogPacketReceivingUnknownPacket((byte)type);
                        _serialPort.ReadExisting();
                        continue;
                }
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogTransportDisconnected("Rx");
                return;
            }

            _logger?.LogTransportWithError(e, "Rx", e.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public void Initialize(Action<HciPacket> onReceived)
    {
        _ = Task.Run(RunTx, _cancelToken);
        _ = Task.Run(() => RunRx(onReceived), _cancelToken);
        _serialPort.Open();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposing)
            return;
        _isDisposing = true;
        _cancelSource.Cancel();
        _serialPort.Dispose();
    }

    /// <inheritdoc />
    public void Enqueue(IHciPacket packet)
    {
        _cancelToken.ThrowIfCancellationRequested();
        _txQueue.Enqueue(packet);
    }
}
