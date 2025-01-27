using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Windows.Foundation;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerService(GattServerPeer peer, GattDeviceService winService, ILogger<WinGattServerService> logger)
    : GattServerService(peer, new BleUuid(winService.Uuid, inferType: true), logger)
{
    private readonly GattDeviceService _winService = winService;

    private IObservable<IGattServerCharacteristic> DiscoverCharacteristic(Func<IAsyncOperation<GattCharacteristicsResult>> getServices)
    {
        return Observable.Create<IGattServerCharacteristic>(async (observer, cancellationToken) =>
        {
            DeviceAccessStatus accessStatus = await _winService.RequestAccessAsync()
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
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

                    foreach (GattCharacteristic gattCharacteristic in result.Characteristics)
                    {
                        observer.OnNext(new WinGattServerCharacteristic(this, gattCharacteristic, LoggerFactory.CreateLogger<WinGattServerCharacteristic>()));
                    }
                }, observer.OnError, observer.OnCompleted);
        });
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsAsyncCore()
    {
        return DiscoverCharacteristic(() => _winService.GetCharacteristicsAsync());
    }

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicAsyncCore(BleUuid uuid)
    {
        return DiscoverCharacteristic(() => _winService.GetCharacteristicsForUuidAsync(uuid.Value));
    }
}