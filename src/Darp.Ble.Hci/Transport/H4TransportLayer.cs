using System.Buffers;
using System.Buffers.Binary;
using System.IO.Ports;
using System.Threading.Channels;
using Darp.Ble.Hci.Package;
using Microsoft.Extensions.Logging;
using HciPacketInfo = (int HeaderLength, int PayloadLengthOffset, int PayloadLengthSize);

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
/// <param name="portName"> The name of the serial port </param>
/// <param name="logger"> An optional logger </param>
public sealed class H4TransportLayer(string portName, ILogger<H4TransportLayer>? logger) : ITransportLayer
{
    private readonly ILogger<H4TransportLayer>? _logger = logger;
    private readonly SerialPort _serialPort = new(portName);
    private readonly Channel<IHciPacket> _txQueue = Channel.CreateUnbounded<IHciPacket>();
    private readonly CancellationTokenSource _cancelSource = new();
    private bool _isDisposing;
    private Task? _rxTask;
    private Task? _txTask;

    private CancellationToken StopToken => _cancelSource.Token;

    /// <inheritdoc />
    public ValueTask InitializeAsync(Action<HciPacket> onReceived, CancellationToken cancellationToken)
    {
        if (_txTask is not null || _rxTask is not null)
            throw new InvalidOperationException("Initialization can only be done once");
        _serialPort.Open();
        _txTask = Task.Run(RunTx, cancellationToken);
        _rxTask = Task.Run(() => RunRx(onReceived), cancellationToken);
        return ValueTask.CompletedTask;
    }

    private async Task RunTx()
    {
        try
        {
            while (!StopToken.IsCancellationRequested)
            {
                IHciPacket packet = await _txQueue.Reader.ReadAsync(StopToken).ConfigureAwait(false);
                int packetLength = 1 + packet.GetByteCount();
                byte[] bytes = ArrayPool<byte>.Shared.Rent(packetLength);
                try
                {
                    bytes[0] = (byte)packet.PacketType;
                    if (!packet.TryWriteLittleEndian(bytes.AsSpan(start: 1)))
                    {
                        _logger?.LogPacketSendingErrorEncoding(packet);
                        continue;
                    }
                    Memory<byte> writeMemory = bytes.AsMemory(0, packetLength);
                    await _serialPort.BaseStream.WriteAsync(writeMemory, StopToken).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytes);
                }
            }
        }
#pragma warning disable CA1031 // We want to catch any exception here to make sure we dont accidentally kill the app
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogH4TransportDisconnected("Tx");
                return;
            }
            _logger?.LogH4TransportWithError(e, "Tx", e.Message);
        }
#pragma warning restore CA1031
    }

    private async Task RunRx(Action<HciPacket> onReceived)
    {
        // Longest allowed memory
        Memory<byte> typeBuffer = new byte[1];
        Memory<byte> buffer = new byte[Constants.HciPacketMaxHeaderLength + Constants.HciPacketMaxPayloadLength];
        try
        {
            while (!StopToken.IsCancellationRequested)
            {
                if (!_serialPort.IsOpen)
                {
                    await Task.Delay(50, StopToken).ConfigureAwait(false);
                    continue;
                }
                // Read Type
                await _serialPort.BaseStream.ReadExactlyAsync(typeBuffer, StopToken).ConfigureAwait(false);
                var type = (HciPacketType)typeBuffer.Span[0];
                HciPacketInfo packetInfo;
                switch (type)
                {
                    case HciPacketType.HciEvent:
                        packetInfo = Constants.HciEventPacketInfo;
                        break;
                    case HciPacketType.HciAclData:
                        packetInfo = Constants.HciAclPacketInfo;
                        break;
                    // HCI Commands should be received by the Controller only
                    case HciPacketType.HciCommand:
                    // Other packet types are not supported yet
                    default:
                        string remaining = _serialPort.ReadExisting();
                        _logger?.LogPacketReceivingUnknownPacket((byte)type, remaining);
                        throw new InvalidOperationException(
                            $"Received invalid packet type: {type}. This is not supported"
                        );
                }
                await RunRxPacket(buffer, type, packetInfo, onReceived).ConfigureAwait(false);
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
        {
            if (_isDisposing)
            {
                _logger?.LogH4TransportDisconnected("Rx");
                return;
            }

            _logger?.LogH4TransportWithError(e, "Rx", e.Message);
        }
#pragma warning restore CA1031
    }

    private async ValueTask RunRxPacket(
        Memory<byte> buffer,
        HciPacketType packetType,
        HciPacketInfo packetInfo,
        Action<HciPacket> onReceived
    )
    {
        int headerLength = packetInfo.HeaderLength;
        // Read Header
        await _serialPort.BaseStream.ReadExactlyAsync(buffer[..headerLength], StopToken).ConfigureAwait(false);
        ushort payloadLength = packetInfo.PayloadLengthSize switch
        {
            1 => buffer.Span[packetInfo.PayloadLengthOffset],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(buffer.Span.Slice(packetInfo.PayloadLengthOffset, 2)),
            _ => throw new InvalidOperationException("Invalid type config. Only sizes of 1 or 2 are allowed"),
        };
        ArgumentOutOfRangeException.ThrowIfGreaterThan(payloadLength, Constants.HciPacketMaxPayloadLength);

        // Read Payload
        Memory<byte> payloadBuffer = buffer[headerLength..(headerLength + payloadLength)];
        await _serialPort.BaseStream.ReadExactlyAsync(payloadBuffer, StopToken).ConfigureAwait(false);
        onReceived(new HciPacket(packetType, buffer[..(headerLength + payloadLength)].Span));
    }

    /// <inheritdoc />
    public void Enqueue(IHciPacket packet)
    {
        ObjectDisposedException.ThrowIf(_isDisposing, this);
        // Writing to an unbounded channel should work, always
        _ = _txQueue.Writer.TryWrite(packet);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposing)
            return;
        _isDisposing = true;
        // Cancel the token first to signal cancellation
        await _cancelSource.CancelAsync().ConfigureAwait(false);
        // Close the serial port to interrupt any pending read/write operations
        // (ReadExactlyAsync does not honor the cancel token)
        try
        {
            _serialPort.Close();
        }
        catch
        {
            // Ignore errors during close
        }
        // Wait for tasks to complete after port is closed
        if (_rxTask is not null)
            await _rxTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (_txTask is not null)
            await _txTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _cancelSource.Dispose();
        // Dispose of the serial port and complete the writer
        _serialPort.Dispose();
        _txQueue.Writer.TryComplete();
        _logger?.LogH4TransportDisposed();
    }
}
