using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

internal sealed class HciHostBleCentral(HciHostBleDevice device, ILogger<HciHostBleCentral> logger)
    : BleCentral(device, logger)
{
    private readonly HciDevice _device = device.HciDevice;

    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(
        BleAddress address,
        BleConnectionParameters connectionParameters,
        BleObservationParameters observationParameters
    )
    {
        var scanInterval = (ushort)observationParameters.ScanInterval;
        var scanWindow = (ushort)observationParameters.ScanWindow;
        var interval = (ushort)connectionParameters.ConnectionInterval;
        return Observable.FromAsync<GattServerPeer>(async token =>
        {
            AclConnection connection = await _device
                .ConnectAsync((byte)address.Type, (ulong)address.Value, token)
                .ConfigureAwait(false);

            return new HciHostGattServerPeer(
                this,
                connection,
                address,
                ServiceProvider.GetLogger<HciHostGattServerPeer>()
            );
        });
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerPeer> DoAfterConnection(IObservable<IGattServerPeer> source) =>
        source
            .Select(peer =>
            {
                return Observable.FromAsync<IGattServerPeer>(async token =>
                {
                    await RunInitializationStepAsync((HciHostGattServerPeer)peer, token).ConfigureAwait(false);
                    return peer;
                });
            })
            .Concat();

    private async Task RunInitializationStepAsync(HciHostGattServerPeer peer, CancellationToken token)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, peer.Connection.DisconnectToken);
        try
        {
            await peer.ReadPhyAsync(token).ConfigureAwait(false);
            await peer.RequestExchangeMtuAsync(65, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (HciConnectionDisconnectedException exception)
        {
            throw new BleCentralConnectionInitializationFailedException(
                this,
                peer.Address,
                peer.ConnectionHandle,
                exception
            );
        }
        catch (Exception exception)
        {
            throw new BleCentralConnectionInitializationFailedException(
                this,
                peer.Address,
                peer.ConnectionHandle,
                exception
            );
        }
    }
}
