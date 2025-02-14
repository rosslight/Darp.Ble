using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientPeer(
    WinBlePeripheral peripheral,
    GattSession session,
    BleAddress address,
    ILogger<WinGattClientPeer> logger
) : GattClientPeer(peripheral, address, logger)
{
    private readonly GattSession _session = session;

    public override bool IsConnected => _session.SessionStatus is GattSessionStatus.Active;

    public override IObservable<Unit> WhenDisconnected =>
        Observable
            .FromEventPattern<
                TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs>,
                GattSession,
                GattSessionStatusChangedEventArgs
            >(
                addHandler => _session.SessionStatusChanged += addHandler,
                removeHandler => _session.SessionStatusChanged -= removeHandler
            )
            .Where(x => x.EventArgs.Status is GattSessionStatus.Closed)
            .Select(_ => Unit.Default);
}
