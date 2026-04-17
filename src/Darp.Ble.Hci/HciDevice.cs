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

/// <summary>
/// Defines the packet queue and state exposed by an ACL connection.
/// </summary>
public interface IAclConnection : IMessageSinkProvider
{
    /// <summary>Gets the host that owns the connection.</summary>
    HciHost Host { get; }

    /// <summary>Gets the controller-assigned connection handle.</summary>
    ushort ConnectionHandle { get; }

    /// <summary>Gets the active ATT MTU for the connection.</summary>
    ushort AttMtu { get; }

    /// <summary>Gets the ACL packet queue used to transmit packets.</summary>
    IAclPacketQueue AclPacketQueue { get; }

    /// <summary>Gets the L2CAP assembler used for incoming data.</summary>
    IL2CapAssembler L2CapAssembler { get; }

    /// <summary>Gets a token that is canceled when the connection is disconnected.</summary>
    CancellationToken DisconnectToken { get; }

    /// <summary>Gets the server address associated with the connection.</summary>
    ulong ServerAddress { get; }

    /// <summary>Gets the client address associated with the connection.</summary>
    ulong ClientAddress { get; }

    /// <summary>Gets the logger used for connection-specific diagnostics.</summary>
    protected internal ILogger Logger { get; }
}

/// <summary>
/// Represents an L2CAP payload together with its channel identifier.
/// </summary>
/// <param name="channelId">The L2CAP channel identifier.</param>
/// <param name="pdu">The L2CAP payload.</param>
public readonly ref struct L2CapPdu(ushort channelId, ReadOnlySpan<byte> pdu)
{
    /// <summary>Gets the L2CAP channel identifier.</summary>
    public ushort ChannelId { get; } = channelId;

    /// <summary>Gets the L2CAP payload.</summary>
    public ReadOnlySpan<byte> Pdu { get; } = pdu;
}

/// <summary>
/// Represents the payload of a generic HCI event packet.
/// </summary>
[BinaryObject]
public readonly partial struct HciPacketEvent
{
    /// <summary>Gets the event code reported by the controller.</summary>
    public required HciEventCode EventCode { get; init; }

    /// <summary>Gets the number of bytes contained in <see cref="DataBytes"/>.</summary>
    public required byte ParameterTotalLength { get; init; }

    /// <summary>Gets the raw event parameter bytes.</summary>
    [BinaryLength(nameof(ParameterTotalLength))]
    public required ReadOnlyMemory<byte> DataBytes { get; init; }
}

/// <summary>
/// Represents a BLE HCI device together with its host, transport, and active connections.
/// </summary>
public sealed class HciDevice : IAsyncDisposable
{
    private readonly ITransportLayer _transportLayer;
    private readonly ILoggerFactory? _loggerFactory;
    private bool _isInitializing;
    private readonly ConcurrentDictionary<ushort, AclConnection> _connections = [];

    /// <summary>Gets the HCI host used to send commands and receive events.</summary>
    public HciHost Host { get; }

    /// <summary>Gets the GATT server exposed by this device.</summary>
    public GattServer GattServer { get; }
    internal ILogger? Logger { get; }

    internal bool IsDisposed { get; private set; }

    /// <summary>Gets the currently configured random device address.</summary>
    public ulong Address { get; private set; }

    /// <summary>Gets the settings used for controller and protocol timeouts.</summary>
    public HciSettings Settings { get; }

    /// <summary>
    /// Initializes a new HCI device wrapper.
    /// </summary>
    /// <param name="transportLayer">The transport used to exchange packets with the controller.</param>
    /// <param name="randomAddress">The initial random device address.</param>
    /// <param name="settings">The settings used for controller and protocol timeouts.</param>
    /// <param name="loggerFactory">An optional logger factory.</param>
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

    /// <summary>Initializes the host and prepares the controller for use.</summary>
    /// <param name="token">Cancels initialization while waiting for controller responses.</param>
    /// <returns>A task that completes when the device has been initialized.</returns>
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

    /// <summary>Resets the host and re-applies the required controller configuration.</summary>
    /// <param name="token">Cancels the reset while waiting for controller responses.</param>
    /// <returns>A task that completes when the reset has finished.</returns>
    public Task ResetAsync(CancellationToken token) => Host.ResetAsync(token);

    /// <summary>Attempts to retrieve an active ACL connection by its handle.</summary>
    /// <param name="connectionHandle">The connection handle to look up.</param>
    /// <param name="connection">The matching connection when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the connection exists; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>Sets the random device address used by the controller.</summary>
    /// <param name="randomAddress">The new random address to configure.</param>
    /// <param name="cancellationToken">Cancels the command while waiting for the controller response.</param>
    /// <returns>A task that completes when the address has been applied.</returns>
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

    /// <summary>Initiates a new LE connection to a peer device.</summary>
    /// <param name="peerAddressType">The peer address type expected by the controller.</param>
    /// <param name="peerAddress">The peer device address.</param>
    /// <param name="token">Cancels the connection attempt.</param>
    /// <returns>The created ACL connection.</returns>
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
