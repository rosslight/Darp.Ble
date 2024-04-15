using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattServerService(GattDeviceService winService)
    : GattServerService(new BleUuid(winService.Uuid, inferType: true))
{
    private readonly GattDeviceService _winService = winService;

    private IObservable<IGattServerCharacteristic> DiscoverCharacteristic(Func<IAsyncOperation<GattCharacteristicsResult>> getServices)
    {
        return Observable.Create<IGattServerCharacteristic>(async (observer, cancellationToken) =>
        {
            DeviceAccessStatus accessStatus = await _winService.RequestAccessAsync().AsTask(cancellationToken);
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
                        observer.OnNext(new WinGattServerCharacteristic(gattCharacteristic));
                    }
                }, observer.OnError, observer.OnCompleted);
        });
    }

    /// <inheritdoc />
    protected override async Task DiscoverCharacteristicsAsyncCore(CancellationToken cancellationToken)
    {
        await DiscoverCharacteristic(() => _winService.GetCharacteristicsAsync())
            .ToTask(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<IGattServerCharacteristic?> DiscoverCharacteristicAsyncCore(BleUuid uuid, CancellationToken cancellationToken)
    {
        return await DiscoverCharacteristic(() => _winService.GetCharacteristicsAsync())
            .FirstAsync()
            .ToTask(cancellationToken);
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        _winService.Dispose();
    }
}