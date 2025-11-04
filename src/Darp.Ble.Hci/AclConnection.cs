using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public readonly record struct AclConnectionParameters(
    ushort ConnectionInterval,
    ushort PeripheralLatency,
    ushort SupervisionTimeout,
    int SubRateFactor,
    int ContinuationNumber
);

public sealed class AclConnection : IDisposable
{
    private readonly CancellationTokenSource _disconnectSource = new();
    private readonly IDisposable _subscription;

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

    public HciDevice Device { get; }
    public ushort ConnectionHandle { get; }
    public UInt48 Address { get; }
    public UInt48 PeerAddress { get; }
    public HciLeConnectionRole Role { get; }
    public AclConnectionParameters Parameters { get; }
    public ushort AttMtu { get; internal set; } = Constants.DefaultAttMtu;
    internal ILogger<AclConnection>? Logger { get; }

    public GattClient GattClient { get; }
    public GattServer GattServer => Device.GattServer;

    public L2CapAssembler Assembler { get; }
    public IAclPacketQueue AclPacketQueue => Device.Host.AclPacketQueue;
    public CancellationToken DisconnectToken => _disconnectSource.Token;

    public async Task SetDataLengthAsync(ushort txOctets, ushort txTime, CancellationToken token = default)
    {
        if (txOctets is < 0x001B or > 0x00FB)
            throw new ArgumentOutOfRangeException(nameof(txOctets));
        if (txTime is < 0x0148 or > 0x4290)
            throw new ArgumentOutOfRangeException(nameof(txOctets));
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

    public async Task<HciLeReadPhyResult> ReadPhyAsync(CancellationToken token = default)
    {
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
            "Received disconnection event for connection 0x{ConnectionHandle:X}. Reason: {Reason}",
            hciEvent.ConnectionHandle,
            hciEvent.Reason
        );
        _disconnectSource.Cancel();
    }

    public async Task DisconnectAsync(
        HciCommandStatus reason = HciCommandStatus.RemoteUserTerminatedConnection,
        CancellationToken token = default
    )
    {
        if (DisconnectToken.IsCancellationRequested)
            return;
        await Device.Host.QueryCommandAsync<HciDisconnectCommand, HciDisconnectionCompleteEvent>(
            new HciDisconnectCommand { ConnectionHandle = ConnectionHandle, Reason = reason },
            timeout: TimeSpan.FromSeconds(2),
            token
        );
    }

    public void Dispose()
    {
        if (DisconnectToken.IsCancellationRequested)
            return;
        _disconnectSource.Cancel();
        _disconnectSource.Dispose();
        _subscription.Dispose();
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
