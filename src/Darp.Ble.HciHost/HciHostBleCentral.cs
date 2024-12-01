using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

public sealed class HciHostBleCentral(HciHostBleDevice device, ILogger? logger) : BleCentral(device, logger)
{
    private readonly Hci.HciHost _host = device.Host;

    protected override IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address, BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        var scanInterval = (ushort)scanParameters.ScanInterval;
        var scanWindow = (ushort)scanParameters.ScanWindow;
        var interval = (ushort)connectionParameters.ConnectionInterval;
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        return Observable.Create<IGattServerPeer>(observer =>
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
            return _host.QueryCommandStatus(packet)
                .SelectMany(status =>
                {
                    if (status.Data.Status is not (HciCommandStatus.PageTimeout or HciCommandStatus.Success))
                    {
                        throw new BleCentralConnectionFailedException(this, $"Started connection but is not pending but {status}");
                    }
                    return _host.WhenHciLeMetaEventPackageReceived;
                })
                .Timeout(timeout)
                .SelectWhereLeMetaEvent<HciLeEnhancedConnectionCompleteV1Event>()
                .Select(x => new HciHostGattServerPeer(_host, x.Data, address, Logger))
                .Subscribe(observer.OnNext, observer.OnError, observer.OnCompleted);
        });
    }

    protected override IObservable<IGattServerPeer> DoAfterConnection(IObservable<IGattServerPeer> source) => source
        .Select(x => ((HciHostGattServerPeer)x).RequestExchangeMtu(65))
        .Concat()
        .Select(x => x.SetDataLength(65, 328))
        .Concat();
}