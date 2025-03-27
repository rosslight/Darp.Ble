using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

internal sealed class HciHostBleCentral(HciHostBleDevice device, ILogger<HciHostBleCentral> logger)
    : BleCentral(device, logger)
{
    private readonly Hci.HciHost _host = device.Host;

    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(
        BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters
    )
    {
        var scanInterval = (ushort)scanParameters.ScanInterval;
        var scanWindow = (ushort)scanParameters.ScanWindow;
        var interval = (ushort)connectionParameters.ConnectionInterval;
        return Observable.FromAsync<GattServerPeer>(async token =>
        {
            var packet = new HciLeExtendedCreateConnectionV1Command
            {
                InitiatorFilterPolicy = 0x00,
                OwnAddressType = 0x01,
                PeerAddressType = (byte)address.Type,
                PeerAddress = (ulong)address.Value,
                InitiatingPhys = 0b1,
                ScanInterval = scanInterval,
                ScanWindow = scanWindow,
                ConnectionIntervalMin = interval,
                ConnectionIntervalMax = interval,
                MaxLatency = 0,
                SupervisionTimeout = 72, // 720ms
                MinCeLength = 0,
                MaxCeLength = 0,
            };
            HciLeEnhancedConnectionCompleteV1Event completeEvent = await _host
                .QueryCommandAsync<HciLeExtendedCreateConnectionV1Command, HciLeEnhancedConnectionCompleteV1Event>(
                    packet,
                    timeout: TimeSpan.FromSeconds(10),
                    cancellationToken: token
                )
                .ConfigureAwait(false);
            return new HciHostGattServerPeer(
                this,
                _host,
                completeEvent,
                address,
                ServiceProvider.GetLogger<HciHostGattServerPeer>()
            );
        });
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerPeer> DoAfterConnection(IObservable<IGattServerPeer> source) =>
        source
            .Select(x => ((HciHostGattServerPeer)x).RequestExchangeMtu(65))
            .Concat()
            .Select(x => x.SetDataLength(65, 328))
            .Concat();
}
