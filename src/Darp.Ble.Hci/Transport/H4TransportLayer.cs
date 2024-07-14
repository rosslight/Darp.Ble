using System.Collections.Concurrent;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci.Transport;

public sealed class H4TransportLayer : ITransportLayer
{
    private readonly ILogger? _logger;
    private readonly SerialPort _serialPort;
    private readonly ConcurrentQueue<IHciPacket> _txQueue;
    private readonly CancellationTokenSource _cancelSource;
    private readonly CancellationToken _cancelToken;
    private readonly Subject<IHciPacket> _rxSubject;
    private bool _isDisposing;

    public H4TransportLayer(string portName, ILogger? logger)
    {
        _logger = logger;
        _serialPort = new SerialPort(portName);
        _txQueue = new ConcurrentQueue<IHciPacket>();
        _rxSubject = new Subject<IHciPacket>();
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
                    await Task.Delay(1, _cancelToken);
                    continue;
                }
                var bytes = new byte[1 + packet.Length];
                bytes[0] = (byte)packet.PacketType;
                if (!packet.TryEncode(bytes.AsSpan()[1..]))
                {
                    _logger?.LogPacketSendingErrorEncoding(packet);
                    continue;
                }

                _logger?.LogPacketSending(packet, bytes);
                await _serialPort.BaseStream.WriteAsync(bytes, _cancelToken);
            }
        }
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogTransportDisconnected("Tx");
            }
            _logger?.LogTransportWithError(e, "Tx", e.Message);
        }
    }

    private async ValueTask RunRxPacket<TPacket>(Memory<byte> buffer, byte payloadLengthIndex)
        where TPacket : IHciPacketImpl<TPacket>, IDecodable<TPacket>
    {
        // _logger?.LogTrace("Starting to read packet of type {Type}", TPacket.Type);
        // Read Header
        await _serialPort.BaseStream.ReadExactlyAsync(buffer[..TPacket.HeaderLength], _cancelToken);
        byte payloadLength = buffer.Span[payloadLengthIndex];
        // Read Payload
        Memory<byte> payloadBuffer = buffer[TPacket.HeaderLength..(TPacket.HeaderLength + payloadLength)];
        await _serialPort.BaseStream.ReadExactlyAsync(payloadBuffer, _cancelToken);
        if (!TPacket.TryDecode(buffer[..(TPacket.HeaderLength + payloadLength)], out TPacket? packet, out _))
        {
            _logger?.LogPacketReceivingDecodingFailed((byte)TPacket.Type, buffer[..(TPacket.HeaderLength + payloadLength)].ToArray(), typeof(TPacket).Name);
            return;
        }

        // _logger?.LogPacketReceiving((byte)packet.PacketType, packet.ToByteArray(), packet.PacketType, packet);
        _rxSubject.OnNext(packet);
    }

    private async ValueTask RunRx()
    {
        // Longest allowed memory
        Memory<byte> buffer = new byte[4 + 255];
        try
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                if (!_serialPort.IsOpen) continue;
                // Read Type
                var type = (HciPacketType)_serialPort.BaseStream.ReadByte();
                switch (type)
                {
                    case HciPacketType.HciEvent:
                        await RunRxPacket<HciEventPacket>(buffer, 1);
                        break;
                    case HciPacketType.HciAclData:
                        await RunRxPacket<HciAclPacket>(buffer, 2);
                        break;
                    case HciPacketType.HciCommand:
                    default:
                        _logger?.LogPacketReceivingUnknownPacket((byte)type);
                        _serialPort.ReadExisting();
                        continue;
                }
            }
            _rxSubject.OnCompleted();
        }
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogTransportDisconnected("Rx");
                _rxSubject.OnCompleted();
                return;
            }
            _logger?.LogTransportWithError(e, "Rx", e.Message);
            _rxSubject.OnError(e);
        }
    }

    public void Initialize()
    {
        _ = Task.Run(RunTx, _cancelToken);
        _ = Task.Run(RunRx, _cancelToken);
        _serialPort.Open();
    }

    public void Dispose()
    {
        if (_isDisposing) return;
        _isDisposing = true;
        _cancelSource.Cancel();
        _serialPort.Dispose();
    }

    public void Enqueue(IHciPacket packet)
    {
        _cancelToken.ThrowIfCancellationRequested();
        _txQueue.Enqueue(packet);
    }

    public IObservable<IHciPacket> WhenReceived()
    {
        _cancelToken.ThrowIfCancellationRequested();
        return _rxSubject.AsObservable();
    }
}