using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble.Mock;



public sealed class BleBroadcasterMock : IBleBroadcaster
{
    private IObservable<AdvertisingData>? _source;
    private AdvertisingParameters? _parameters;

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        BleAddress ownAddress = new(BleAddressType.Public, (UInt48)0xAABBCCDDEEFF);

        IObservable<AdvertisingData> dataSource = _source ?? Observable.Empty<AdvertisingData>();
        return dataSource.Select(data => GapAdvertisement.FromExtendedAdvertisingReport(observer,
            DateTimeOffset.UtcNow,
            _parameters?.Type ?? BleEventType.None,
            ownAddress,
            Physical.Le1M,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            (TxPowerLevel)20,
            (Rssi)(-40),
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            new BleAddress(BleAddressType.NotAvailable, UInt48.Zero),
            data));
    }

    public IDisposable Advertise(AdvertisingSet advertisingSet) => throw new NotImplementedException();
    public IDisposable Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters = null)
    {
        _source = source;
        _parameters = parameters;
        return Disposable.Create(this, self => self._source = null);
    }
}

public sealed class BleMockFactory : IBleFactory
{
    public required Func<BleBroadcasterMock, IBlePeripheral, Task> OnConfigure { get; init; }

    public IEnumerable<IBleDevice> EnumerateDevices(IObserver<(BleDevice, LogEvent)>? logger)
    {
        yield return new MockBleDevice(OnConfigure, logger);
    }
}