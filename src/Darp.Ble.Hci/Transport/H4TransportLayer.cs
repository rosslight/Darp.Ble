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
    /// <summary>Gets the HCI packet type.</summary>
    public HciPacketType PacketType { get; } = packetType;

    /// <summary>Gets the packet body without the packet type byte (header + payload).</summary>
    public ReadOnlySpan<byte> Pdu { get; } = pdu;
}

/// <summary> A transport layer which sends HCI packets via a <see cref="SerialPort"/> </summary>
/// <param name="portName"> The name of the serial port </param>
/// <param name="logger"> An optional logger </param>
public sealed class H4TransportLayer(string portName, ILogger<H4TransportLayer>? logger) : ITransportLayer
{
    private readonly ILogger<H4TransportLayer>? _logger = logger;
    private readonly SerialPort _serialPort = new(portName);

    // Each packet is serialized into a pooled array when it is queued, so the caller may reuse its buffers at once
    private readonly Channel<(byte[] Bytes, int Length)> _txQueue = Channel.CreateUnbounded<(byte[], int)>(
        new UnboundedChannelOptions { SingleReader = true }
    );
    private readonly CancellationTokenSource _cancelSource = new();

    // Set once disposal started; the loops then end without reporting a failure
    private bool _isDisposing;

    // Both loops fail when the port breaks; only the first reports it
    private int _isFaulted;

    // The loops, set once by InitializeAsync; disposal waits for them
    private Task? _rxTask;
    private Task? _txTask;

    private CancellationToken StopToken => _cancelSource.Token;

    /// <inheritdoc />
    public ValueTask InitializeAsync(
        Action<HciPacket> onReceived,
        Action<Exception> onError,
        CancellationToken cancellationToken
    )
    {
        if (_txTask is not null || _rxTask is not null)
            throw new InvalidOperationException("Initialization can only be done once");
        _serialPort.Open();
        _txTask = Task.Run(() => RunTx(onError), cancellationToken);
        _rxTask = Task.Run(() => RunRx(onReceived, onError), cancellationToken);
        return ValueTask.CompletedTask;
    }

    private void ReportTransportFailure(Exception exception, string direction, Action<Exception> onError)
    {
        if (Interlocked.Exchange(ref _isFaulted, 1) != 0)
            return;

        _logger?.LogH4TransportWithError(exception, direction, exception.Message);
        try
        {
            _cancelSource.Cancel();
        }
        catch
        {
            // Ignore cancellation errors while reporting a fatal transport failure
        }

        try
        {
            _serialPort.Close();
        }
        catch
        {
            // Ignore close errors while reporting a fatal transport failure
        }

        onError(exception);
    }

    private async Task RunTx(Action<Exception> onError)
    {
        try
        {
            while (!StopToken.IsCancellationRequested)
            {
                (byte[] bytes, int length) = await _txQueue.Reader.ReadAsync(StopToken).ConfigureAwait(false);
                try
                {
                    await _serialPort.BaseStream.WriteAsync(bytes.AsMemory(0, length), StopToken).ConfigureAwait(false);
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
            ReportTransportFailure(e, "Tx", onError);
        }
#pragma warning restore CA1031
    }

    private async Task RunRx(Action<HciPacket> onReceived, Action<Exception> onError)
    {
        // Longest allowed memory
        Memory<byte> typeBuffer = new byte[1];
        Memory<byte> buffer = new byte[Constants.HciPacketMaxHeaderLength + Constants.HciPacketMaxPayloadLength];
        try
        {
            while (!StopToken.IsCancellationRequested)
            {
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

            ReportTransportFailure(e, "Rx", onError);
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
        ArgumentNullException.ThrowIfNull(packet);
        ObjectDisposedException.ThrowIf(_isDisposing, this);
        int length = 1 + packet.GetByteCount();
        byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
        bytes[0] = (byte)packet.PacketType;
        if (!packet.TryWriteLittleEndian(bytes.AsSpan(1, length - 1)))
        {
            ArrayPool<byte>.Shared.Return(bytes);
            _logger?.LogPacketSendingErrorEncoding(packet);
            return;
        }
        // Writing to an unbounded channel should work, always
        if (!_txQueue.Writer.TryWrite((bytes, length)))
            ArrayPool<byte>.Shared.Return(bytes);
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
        // Packets that were never written still hold pooled arrays
        while (_txQueue.Reader.TryRead(out (byte[] Bytes, int Length) unsent))
            ArrayPool<byte>.Shared.Return(unsent.Bytes);
        _logger?.LogH4TransportDisposed();
    }
}
