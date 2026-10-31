using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.HciHost.Connections;

/// <summary> Connects to peripherals with the controller's extended create connection </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The semaphore never creates its wait handle"
)]
internal sealed class HciHostCentralDriver(HciDevice device) : ICentralDriver
{
    private static readonly TimeSpan MinSupervisionTimeout = TimeSpan.FromMilliseconds(720);

    private readonly HciDevice _device = device;

    // The controller initiates one connection at a time; further attempts wait for their turn.
    // Without its wait handle the semaphore holds nothing to dispose
#pragma warning disable CA2213
    private readonly SemaphoreSlim _attempts = new(1, 1);
#pragma warning restore CA2213

    public async Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    )
    {
        ushort interval = HciTiming.ToConnectionIntervalUnits(
            parameters.ConnectionInterval,
            nameof(parameters.ConnectionInterval)
        );
        // Six missed connection events, but not shorter than v1's 720 ms
        TimeSpan supervision = parameters.ConnectionInterval * 6;
        ushort supervisionTimeout = HciTiming.ToSupervisionTimeoutUnits(
            supervision > MinSupervisionTimeout ? supervision : MinSupervisionTimeout,
            "supervision timeout"
        );
        await _attempts.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var attempt = new ConnectAttempt(_device, sink, address);
            HciCommandStatus status = await StartAsync(address, interval, supervisionTimeout, cancellationToken)
                .ConfigureAwait(false);
            if (status is not HciCommandStatus.Success)
            {
                throw new BleConnectException(
                    status is HciCommandStatus.CommandDisallowed or HciCommandStatus.ConnectionAlreadyExists
                        ? BleConnectErrorKind.Busy
                        : BleConnectErrorKind.Rejected,
                    $"The controller refused to connect: {status}",
                    (int)status
                );
            }
            try
            {
                return await attempt.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CancelAsync(attempt).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _attempts.Release();
        }
    }

    /// <summary> Asks the controller to connect; a broken or silent controller is a connect error </summary>
    private async Task<HciCommandStatus> StartAsync(
        BleAddress address,
        ushort interval,
        ushort supervisionTimeout,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await _device
                .Host.QueryCommandStatusAsync(
                    new HciLeExtendedCreateConnectionV1Command
                    {
                        InitiatorFilterPolicy = 0x00,
                        OwnAddressType = 0x01,
                        PeerAddressType = (byte)address.Type,
                        PeerAddress = address.Value.ToUInt64(),
                        InitiatingPhys = 0b1,
                        ScanInterval = 96,
                        ScanWindow = 96,
                        ConnectionIntervalMin = interval,
                        ConnectionIntervalMax = interval,
                        MaxLatency = 0,
                        SupervisionTimeout = supervisionTimeout,
                        MinCeLength = 0,
                        MaxCeLength = 0,
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (TimeoutException e)
        {
            throw new BleConnectException(
                BleConnectErrorKind.TimedOut,
                "The controller did not answer the connect command",
                innerException: e
            );
        }
        catch (Hci.Exceptions.HciException e)
        {
            throw new BleConnectException(
                BleConnectErrorKind.Unreachable,
                "The controller could not start connecting",
                innerException: e
            );
        }
    }

    /// <summary> Cancels the attempt at the controller; a link that came up meanwhile is taken down again </summary>
    private async Task CancelAsync(ConnectAttempt attempt)
    {
        // Command Disallowed: the attempt ended meanwhile, and its event is on the way
        await _device
            .Host.QueryCommandCompletionAsync<HciLeCreateConnectionCancelCommand, HciLeCreateConnectionCancelResult>(
                default,
                CancellationToken.None
            )
            .ConfigureAwait(false);
        HciHostConnectionDriver? raced;
        try
        {
            raced = await attempt
                .Task.WaitAsync(_device.Settings.CommandTimeout, _device.TimeProvider, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (e is OperationCanceledException or BleConnectException or TimeoutException)
        {
            return;
        }
        await raced.DisposeAsync().ConfigureAwait(false);
    }
}
