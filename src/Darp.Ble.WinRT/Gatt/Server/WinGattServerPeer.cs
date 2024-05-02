using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Windows.Foundation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

/// <inheritdoc />
public sealed class WinGattServerPeer : GattServerPeer
{
    private readonly BluetoothLEDevice _winDev;

    internal WinGattServerPeer(BluetoothLEDevice winDev, ILogger? logger)
        : base(BleHelper.GetBleAddress(winDev.BluetoothAddress, winDev.BluetoothAddressType), logger)
    {
        _winDev = winDev;
        WhenConnectionStatusChanged = Observable.FromEventPattern<TypedEventHandler<BluetoothLEDevice, object>, BluetoothLEDevice, object>(
                addHandler => winDev.ConnectionStatusChanged += addHandler,
                removeHandler => winDev.ConnectionStatusChanged -= removeHandler)
            .Where(x => x.Sender is not null)
            .Select(x => x.Sender!.ConnectionStatus is BluetoothConnectionStatus.Connected
                ? ConnectionStatus.Connected
                : ConnectionStatus.Disconnected);
    }

    /// <inheritdoc />
    public override IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }

    private IObservable<IGattServerService> DiscoverService(Func<IAsyncOperation<GattDeviceServicesResult>> getServices)
    {
        return Observable.Create<IGattServerService>(async (observer, cancellationToken) =>
        {
            DeviceAccessStatus accessStatus = await _winDev.RequestAccessAsync().AsTask(cancellationToken);
            if (accessStatus is not DeviceAccessStatus.Allowed)
            {
                observer.OnError(new Exception($"Access request disallowed: {accessStatus}..."));
                return Disposable.Empty;
            }
            return getServices().ToObservable()
                .Subscribe(result =>
                {
                    if (result.Status is not GattCommunicationStatus.Success)
                    {
                        observer.OnError(new Exception($"Could not query new services for device - got result {result.Status} ({result.ProtocolError})"));
                        return;
                    }

                    foreach (GattDeviceService gattDeviceService in result.Services)
                    {
                        observer.OnNext(new WinGattServerService(gattDeviceService, Logger));
                    }
                }, observer.OnError, observer.OnCompleted);
        });
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServicesCore()
    {
        return DiscoverService(() => _winDev.GetGattServicesAsync());
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid)
    {
        return DiscoverService(() => _winDev.GetGattServicesForUuidAsync(uuid.Value));
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        _winDev.Dispose();
    }
}