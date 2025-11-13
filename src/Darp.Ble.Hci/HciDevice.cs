using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public interface IAclConnection : IMessageSinkProvider
{
    HciHost Host { get; }
    ushort ConnectionHandle { get; }
    ushort AttMtu { get; }
    IAclPacketQueue AclPacketQueue { get; }
    IL2CapAssembler L2CapAssembler { get; }

    /// <summary> A cancellationToken that will fire when the connection was disconnected </summary>
    CancellationToken DisconnectToken { get; }
    ulong ServerAddress { get; }
    ulong ClientAddress { get; }
    protected internal ILogger Logger { get; }
}

public readonly ref struct L2CapPdu(ushort channelId, ReadOnlySpan<byte> pdu)
{
    public ushort ChannelId { get; } = channelId;
    public ReadOnlySpan<byte> Pdu { get; } = pdu;
}

[BinaryObject]
public readonly partial struct HciPacketEvent
{
    public required HciEventCode EventCode { get; init; }
    public required byte ParameterTotalLength { get; init; }

    [BinaryLength(nameof(ParameterTotalLength))]
    public required ReadOnlyMemory<byte> DataBytes { get; init; }
}

/// <summary> The HCI Device </summary>
public sealed class HciDevice : IAsyncDisposable
{
    private readonly ITransportLayer _transportLayer;
    private readonly ILoggerFactory? _loggerFactory;
    private bool _isInitializing;
    private readonly ConcurrentDictionary<ushort, AclConnection> _connections = [];

    /// <summary> The HCI Host </summary>
    public HciHost Host { get; }

    /// <summary> The GATT Server </summary>
    public GattServer GattServer { get; }
    internal ILogger? Logger { get; }

    internal bool IsDisposed { get; private set; }

    /// <summary> The random address </summary>
    public ulong Address { get; private set; }

    /// <summary> Settings to be used </summary>
    public HciSettings Settings { get; }

    /// <summary> The HCI Host </summary>
    /// <param name="transportLayer"> The transport layer </param>
    /// <param name="randomAddress"> The random address of the device </param>
    /// <param name="settings"> Settings for timings, etc </param>
    /// <param name="loggerFactory"> An optional logger </param>
    public HciDevice(
        ITransportLayer transportLayer,
        ulong randomAddress,
        HciSettings settings,
        ILoggerFactory? loggerFactory
    )
    {
        _transportLayer = transportLayer;
        _loggerFactory = loggerFactory;
        Address = randomAddress;
        Settings = settings;
        Logger = loggerFactory?.CreateLogger<HciDevice>();
        Host = new HciHost(this, transportLayer, loggerFactory?.CreateLogger<HciHost>());
        GattServer = new GattServer(this, loggerFactory?.CreateLogger<GattServer>());
    }

    /// <summary> Initialize the host </summary>
    /// <param name="token"> The cancellationToken to cancel the operation </param>
    public async Task InitializeAsync(CancellationToken token)
    {
        if (_isInitializing)
            throw new InvalidOperationException("Already initialized");
        try
        {
            _isInitializing = true;
            await Host.ResetAsync(token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _isInitializing = false;
            throw;
        }
    }

    public Task ResetAsync(CancellationToken token) => Host.ResetAsync(token);

    public bool TryGetConnection(ushort connectionHandle, [NotNullWhen(true)] out AclConnection? connection)
    {
        return _connections.TryGetValue(connectionHandle, out connection);
    }

    internal void RemoveConnection(ushort connectionHandle)
    {
        bool isRemoved = _connections.TryRemove(connectionHandle, out AclConnection? removedConnection);
        Debug.Assert(isRemoved);
        removedConnection?.Dispose();
    }

    internal void OnHciLeEnhancedConnectionCompleteV1EventPacket(HciLeEnhancedConnectionCompleteV1Event evt)
    {
        var connection = new AclConnection(
            this,
            evt.ConnectionHandle,
            Address,
            evt.PeerAddress,
            evt.Role,
            new AclConnectionParameters(evt.ConnectionInterval, evt.PeripheralLatency, evt.SupervisionTimeout, 0, 0),
            _loggerFactory
        );
        bool isAdded = _connections.TryAdd(evt.ConnectionHandle, connection);
        Debug.Assert(isAdded, $"Connection {connection.ConnectionHandle} could not be registered at host!");
    }

    /// <summary> Writes a new, random address to the host </summary>
    /// <param name="randomAddress"></param>
    /// <param name="cancellationToken"></param>
    public async Task SetRandomAddressAsync(ulong randomAddress, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!_isInitializing)
        {
            Address = randomAddress;
            return;
        }
        await Host.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(
                new HciLeSetRandomAddressCommand(randomAddress),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        Address = randomAddress;
    }

    public async Task<AclConnection> ConnectAsync(byte peerAddressType, UInt48 peerAddress, CancellationToken token)
    {
        var packet = new HciLeExtendedCreateConnectionV1Command
        {
            InitiatorFilterPolicy = 0x00,
            OwnAddressType = 0x01,
            PeerAddressType = peerAddressType,
            PeerAddress = peerAddress,
            InitiatingPhys = 0b1,
            ScanInterval = 96,
            ScanWindow = 96,
            ConnectionIntervalMin = 12,
            ConnectionIntervalMax = 24,
            MaxLatency = 0,
            SupervisionTimeout = 72, // 720ms
            MinCeLength = 0,
            MaxCeLength = 0,
        };
        HciLeEnhancedConnectionCompleteV1Event evt = await Host.QueryCommandAsync<
            HciLeExtendedCreateConnectionV1Command,
            HciLeEnhancedConnectionCompleteV1Event
        >(packet, timeout: TimeSpan.FromSeconds(30), cancellationToken: token)
            .ConfigureAwait(false);
        return TryGetConnection(evt.ConnectionHandle, out AclConnection? connection)
            ? connection
            : throw new Exception($"Connection {evt.ConnectionHandle} was not found");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        IsDisposed = true;
        await _transportLayer.DisposeAsync().ConfigureAwait(false);
    }
}

internal sealed partial class AttResponseMessageSinkProvider<TResponse>(AttOpCode expectedOpCode)
    where TResponse : IAttPdu
{
    private readonly AttOpCode _expectedOpCode = expectedOpCode;

    private readonly TaskCompletionSource<AttResponse<TResponse>> _tcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    [MessageSink]
    private void OnValue<T>(T value)
        where T : allows ref struct
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (typeof(T) == typeof(TResponse))
        {
            TResponse response = Unsafe.As<T, TResponse>(ref value);
            _tcs.TrySetResult(AttResponse<TResponse>.Ok(response));
        }
        else if (typeof(T) == typeof(AttErrorRsp))
        {
            AttErrorRsp errorResponse = Unsafe.As<T, AttErrorRsp>(ref value);
            if (_expectedOpCode != errorResponse.RequestOpCode)
                return;
            _tcs.TrySetResult(AttResponse<TResponse>.Fail(errorResponse));
        }
    }

    public Task<AttResponse<TResponse>> Task => _tcs.Task;
}
