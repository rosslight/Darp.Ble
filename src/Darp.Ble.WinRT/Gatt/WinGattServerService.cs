using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT.Gatt;

public sealed class WinGattServerService(GattDeviceService winService) : IPlatformSpecificGattServerService
{
    private readonly GattDeviceService _winService = winService;
    public BleUuid Uuid { get; } = new(winService.Uuid, inferType: true);

    private IObservable<IPlatformSpecificGattServerCharacteristic> DiscoverCharacteristic(Func<IAsyncOperation<GattCharacteristicsResult>> getServices)
    {
        return Observable.Create<IPlatformSpecificGattServerCharacteristic>(async (observer, cancellationToken) =>
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

    public async Task DiscoverCharacteristicsAsync(CancellationToken cancellationToken)
    {
        await DiscoverCharacteristic(() => _winService.GetCharacteristicsAsync())
            .ToTask(cancellationToken);
    }

    public async Task<IPlatformSpecificGattServerCharacteristic?> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken)
    {
        return await DiscoverCharacteristic(() => _winService.GetCharacteristicsAsync())
            .FirstAsync()
            .ToTask(cancellationToken);
    }
}