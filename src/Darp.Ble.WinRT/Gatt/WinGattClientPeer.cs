using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientPeer(GattSession session, BleAddress address) : IGattClientPeer
{
    private readonly GattSession _session = session;
    public BleAddress Address { get; } = address;
    public bool IsConnected => _session.SessionStatus is GattSessionStatus.Active;
    public IObservable<Unit> WhenDisconnected => Observable
        .FromEventPattern<TypedEventHandler<GattSession,GattSessionStatusChangedEventArgs>,
            GattSession,GattSessionStatusChangedEventArgs>(
            addHandler => _session.SessionStatusChanged += addHandler,
            removeHandler => _session.SessionStatusChanged -= removeHandler)
        .Where(x => x.EventArgs.Status is GattSessionStatus.Closed)
        .Select(_ => Unit.Default);
}