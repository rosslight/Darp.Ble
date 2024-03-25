using System.Collections.Concurrent;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Transport;

public sealed class H4TransportLayer : ITransportLayer
{
    //private readonly IObserver<LogEvent> _logger;
    private readonly SerialPort _serialPort;
    private readonly ConcurrentQueue<IHciPacket> _txQueue;
    private readonly CancellationTokenSource _cancelSource;
    private readonly CancellationToken _cancelToken;
    private readonly Subject<IHciPacket> _rxSubject;
    private bool _isDisposing;

    public H4TransportLayer(string portName)//, IObserver<LogEvent> logger)
    {
        //_logger = logger;
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
                if (!_serialPort.IsOpen) continue;
                if (_txQueue.IsEmpty) continue;
                if (!_txQueue.TryDequeue(out IHciPacket? packet)) continue;
                var bytes = new byte[1 + packet.Length];
                bytes[0] = (byte)packet.PacketType;
                if (!packet.TryEncode(bytes.AsSpan()[1..]))
                {
                    //_logger.Verbose("H4Transport: Could not send packet {@Packet} due to error while encoding", packet);
                    continue;
                }

                // _logger.Verbose("H4Transport: Sending packet {@Packet} with bytes 0x{@Bytes}", packet, bytes);
                await _serialPort.BaseStream.WriteAsync(bytes, _cancelToken);
            }
        }
        catch (Exception e)
        {
            if (_isDisposing)
            {
                //_logger.Verbose("H4Transport: Tx disconnected");
            }
            //_logger.Fatal(e, "H4Transport: Tx died due to exception {Message}. This error is not recoverable!", e.Message);
        }
    }

    private async ValueTask RunRxPacket<TPacket>(Memory<byte> buffer, byte payloadLengthIndex)
        where TPacket : IHciPacketImpl<TPacket>, IDecodable<TPacket>
    {
        //Log.Logger.Verbose("Starting to read packet of type {Type}", TPacket.Type);
        // Read Header
        await _serialPort.BaseStream.ReadExactlyAsync(buffer[..TPacket.HeaderLength], _cancelToken);
        byte payloadLength = buffer.Span[payloadLengthIndex];
        // Read Payload
        Memory<byte> payloadBuffer = buffer[TPacket.HeaderLength..(TPacket.HeaderLength + payloadLength)];
        await _serialPort.BaseStream.ReadExactlyAsync(payloadBuffer, _cancelToken);
        if (!TPacket.TryDecode(buffer[..(TPacket.HeaderLength + payloadLength)], out TPacket? packet, out _))
        {
            //_logger.Warning("H4Transport: Could not decode bytes 0x{PacketBytes:X2}{Bytes} to match packet {PacketType}",
            //    (byte)TPacket.Type, buffer[..(TPacket.HeaderLength + payloadLength)].ToArray(), typeof(TPacket).Name);
            return;
        }

        //_logger.Verbose("Read bytes 0x{PacketBytes:X2}{Bytes} of {PacketType} packet {@Packet}",
        //    (byte)packet.PacketType, packet.ToByteArray(), packet.PacketType, packet);
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
                    default:
                        //_logger.Warning("H4Transport: Received unknown hci packet of type 0x{Type:X2}. Reading remaining buffer ...", (byte)type);
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
                //_logger.Verbose("H4Transport: Rx disconnected");
                _rxSubject.OnCompleted();
                return;
            }
            //_logger.Fatal(e, "H4Transport: Rx died due to exception {Message}. This error is not recoverable!", e.Message);
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

    public void Enqueue(IHciPacket packet) => _txQueue.Enqueue(packet);

    public IObservable<IHciPacket> WhenReceived() => _rxSubject.AsObservable();
}