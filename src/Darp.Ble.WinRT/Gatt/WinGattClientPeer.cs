using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattClientPeer(GattSession session) : IGattClientPeer
{
    private readonly GattSession _session = session;
    public BleAddress Address { get; } = BleAddress.Parse(session.DeviceId.Id, provider: null);
    public bool IsConnected => _session.SessionStatus is GattSessionStatus.Active;
    public IObservable<Unit> WhenDisconnected => Observable
        .FromEventPattern<TypedEventHandler<GattSession,GattSessionStatusChangedEventArgs>,
            GattSession,GattSessionStatusChangedEventArgs>(
            addHandler => _session.SessionStatusChanged += addHandler,
            removeHandler => _session.SessionStatusChanged -= removeHandler)
        .Where(x => x.EventArgs.Status is GattSessionStatus.Closed)
        .Select(_ => Unit.Default);
}