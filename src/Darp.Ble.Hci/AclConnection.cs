using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary>
/// Represents the negotiated link-layer parameters for an ACL connection.
/// </summary>
/// <param name="ConnectionInterval">The connection interval in controller units.</param>
/// <param name="PeripheralLatency">The number of connection events the peripheral may skip.</param>
/// <param name="SupervisionTimeout">The timeout before the connection is considered lost.</param>
/// <param name="SubRateFactor">The subrate factor applied to the connection.</param>
/// <param name="ContinuationNumber">The continuation number used for subrating.</param>
public readonly record struct AclConnectionParameters(
    ushort ConnectionInterval,
    ushort PeripheralLatency,
    ushort SupervisionTimeout,
    int SubRateFactor,
    int ContinuationNumber
);

/// <summary>
/// Represents an active ACL connection to a peer device.
/// </summary>
public sealed class AclConnection : IDisposable
{
    private readonly CancellationTokenSource _disconnectSource = new();
    private readonly IDisposable _subscription;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new ACL connection wrapper.
    /// </summary>
    /// <param name="device">The device that owns the connection.</param>
    /// <param name="connectionHandle">The controller-assigned connection handle.</param>
    /// <param name="address">The local device address used for the connection.</param>
    /// <param name="peerAddress">The remote peer address.</param>
    /// <param name="role">The local role on the connection.</param>
    /// <param name="parameters">The negotiated connection parameters.</param>
    /// <param name="loggerFactory">An optional logger factory.</param>
    public AclConnection(
        HciDevice device,
        ushort connectionHandle,
        UInt48 address,
        UInt48 peerAddress,
        HciLeConnectionRole role,
        AclConnectionParameters parameters,
        ILoggerFactory? loggerFactory
    )
    {
        Device = device;
        ConnectionHandle = connectionHandle;
        Address = address;
        PeerAddress = peerAddress;
        Role = role;
        Parameters = parameters;
        Logger = loggerFactory?.CreateLogger<AclConnection>();
        Assembler = new L2CapAssembler(connectionHandle, loggerFactory?.CreateLogger<L2CapAssembler>());
        _subscription = Assembler.Subscribe(new AclConnectionSubscription(this));
        GattClient = new GattClient(this);
    }

    /// <summary>Gets the device that owns this connection.</summary>
    public HciDevice Device { get; }

    /// <summary>Gets the controller-assigned connection handle.</summary>
    public ushort ConnectionHandle { get; }

    /// <summary>Gets the local device address used for the connection.</summary>
    public UInt48 Address { get; }

    /// <summary>Gets the remote peer address.</summary>
    public UInt48 PeerAddress { get; }

    /// <summary>Gets the local role on the connection.</summary>
    public HciLeConnectionRole Role { get; }

    /// <summary>Gets the negotiated connection parameters.</summary>
    public AclConnectionParameters Parameters { get; }

    /// <summary>Gets the currently effective ATT MTU for the connection.</summary>
    public ushort AttMtu { get; internal set; } = Constants.DefaultAttMtu;
    internal ILogger<AclConnection>? Logger { get; }

    /// <summary>Gets the GATT client bound to this connection.</summary>
    public GattClient GattClient { get; }

    /// <summary>Gets the local GATT server exposed through the owning device.</summary>
    public GattServer GattServer => Device.GattServer;

    /// <summary>Gets the assembler used to rebuild incoming L2CAP PDUs.</summary>
    public L2CapAssembler Assembler { get; }

    /// <summary>Gets the ACL packet queue used to send packets on this connection.</summary>
    public IAclPacketQueue AclPacketQueue => Device.Host.AclPacketQueue;

    /// <summary>Gets a token that is canceled when the connection is disconnected.</summary>
    public CancellationToken DisconnectToken => _disconnectSource.Token;

    /// <summary>Gets the last disconnection reason reported by the controller, if available.</summary>
    public HciCommandStatus? LastDisconnectionReason { get; private set; }

    /// <summary>Updates the controller's data length settings for this connection.</summary>
    /// <param name="txOctets">The maximum number of payload octets to transmit.</param>
    /// <param name="txTime">The maximum transmit time in microseconds.</param>
    /// <param name="token">Cancels the command while waiting for the controller response.</param>
    /// <returns>A task that completes when the controller accepts the new data length.</returns>
    public async Task SetDataLengthAsync(ushort txOctets, ushort txTime, CancellationToken token = default)
    {
        if (txOctets is < 0x001B or > 0x00FB)
            throw new ArgumentOutOfRangeException(nameof(txOctets));
        if (txTime is < 0x0148 or > 0x4290)
            throw new ArgumentOutOfRangeException(nameof(txOctets));
        ThrowIfDisconnected("set data length");
        await Device
            .Host.QueryCommandCompletionAsync<HciLeSetDataLengthCommand, HciLeSetDataLengthResult>(
                new HciLeSetDataLengthCommand
                {
                    ConnectionHandle = ConnectionHandle,
                    TxOctets = txOctets,
                    TxTime = txTime,
                },
                cancellationToken: token
            )
            .ConfigureAwait(false);
    }

    /// <summary>Reads the currently selected PHYs for this connection.</summary>
    /// <param name="token">Cancels the command while waiting for the controller response.</param>
    /// <returns>The PHY information reported by the controller.</returns>
    public async Task<HciLeReadPhyResult> ReadPhyAsync(CancellationToken token = default)
    {
        ThrowIfDisconnected("read phy");
        return await Device
            .Host.QueryCommandCompletionAsync<HciLeReadPhyCommand, HciLeReadPhyResult>(
                new HciLeReadPhyCommand { ConnectionHandle = ConnectionHandle },
                cancellationToken: token
            )
            .ConfigureAwait(false);
    }

    internal void OnDisconnectEvent(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != ConnectionHandle)
            return;
        Logger?.LogDebug(
            "AclConnection: Received disconnection event for connection 0x{ConnectionHandle:X}. Reason: {Reason}",
            hciEvent.ConnectionHandle,
            hciEvent.Reason
        );
        LastDisconnectionReason = hciEvent.Reason;
        _disconnectSource.Cancel();
    }

    /// <summary>Requests that the controller disconnect this connection.</summary>
    /// <param name="reason">The HCI reason code sent with the disconnect request.</param>
    /// <param name="token">Cancels the command while waiting for the disconnect event.</param>
    /// <returns>A task that completes when the controller confirms the disconnect.</returns>
    public async Task DisconnectAsync(
        HciCommandStatus reason = HciCommandStatus.RemoteUserTerminatedConnection,
        CancellationToken token = default
    )
    {
        if (DisconnectToken.IsCancellationRequested)
            return;
        await Device
            .Host.QueryCommandAsync<HciDisconnectCommand, HciDisconnectionCompleteEvent>(
                new HciDisconnectCommand { ConnectionHandle = ConnectionHandle, Reason = reason },
                timeout: TimeSpan.FromSeconds(2),
                token
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        if (!DisconnectToken.IsCancellationRequested)
            _disconnectSource.Cancel();
        _disconnectSource.Dispose();
        _subscription.Dispose();
    }

    internal HciConnectionDisconnectedException CreateDisconnectedException(
        string operation,
        Exception? innerException = null
    ) => new(ConnectionHandle, operation, LastDisconnectionReason, innerException);

    private void ThrowIfDisconnected(string operation)
    {
        if (DisconnectToken.IsCancellationRequested)
            throw CreateDisconnectedException(operation);
    }
}

internal sealed partial class AclConnectionSubscription(AclConnection connection)
{
    private readonly AclConnection _connection = connection;

    [MessageSink]
    private void OnAttExchangeMtuReq(AttExchangeMtuReq req)
    {
        _connection.GattServer.OnAttExchangeMtuReq(_connection, req);
    }
}
