using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
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
                    var hciPeer = (HciHostGattServerPeer)peer;
                    await hciPeer.ReadPhyAsync(token).ConfigureAwait(false);
                    await hciPeer.RequestExchangeMtuAsync(65, token).ConfigureAwait(false);
                    return peer;
                });
            })
            .Concat();
}
