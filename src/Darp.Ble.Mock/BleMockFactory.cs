using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

public interface IGattServerService
{
    
}

public interface IBlePeripheral
{
    void AddService(IGattServerService peripheral);
}

public sealed class BlePeripheralMock : IBlePeripheral
{
    public void AddService(IGattServerService peripheral) => throw new NotImplementedException();
}

public interface IBleBroadcaster
{
    IDisposable Advertise(AdvertisingSet advertisingSet);
    IDisposable Advertise(IObservable<AdvertisingData> source);
}

public sealed class BleBroadcasterMock : IBleBroadcaster
{
    public BleAddress OwnAddress { get; } = new(BleAddressType.Public, (UInt48)0xAABBCCDDEEFF);

    private IObservable<AdvertisingData>? _source;
    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        IObservable<AdvertisingData> dataSource = _source ?? Observable.Empty<AdvertisingData>();
        return dataSource.Select(data => GapAdvertisement.FromExtendedAdvertisingReport(observer,
            DateTimeOffset.UtcNow,
            BleEventType.None,
            OwnAddress,
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
    public IDisposable Advertise(IObservable<AdvertisingData> source)
    {
        _source = source;
        return Disposable.Create(this, self => self._source = null);
    }
}

public sealed class AdvertisingSet
{
    private readonly BehaviorSubject<AdvertisingSet> _subject;
    private AdvertisingData? _scanResponseData;
    private AdvertisingData _data;

    public AdvertisingSet(int setId, ScanTiming interval)
    {
        _subject = new BehaviorSubject<AdvertisingSet>(this);
        _data = AdvertisingData.Empty;
        SetId = setId;
        Interval = interval;
    }

    public int SetId { get; }

    public ScanTiming Interval { get; init; }
    public AdvertisingData Data { get => _data; [MemberNotNull(nameof(_data))] set => SetAndNotifyIfChanged(ref _data, value); }
    public AdvertisingData? ScanResponseData { get => _scanResponseData; set => SetAndNotifyIfChanged(ref _scanResponseData, value); }

    public IObservable<AdvertisingSet> WhenChanged => _subject.AsObservable();

    private void SetAndNotifyIfChanged<T>([NotNullIfNotNull(nameof(value))] ref T field, T value)
    {
        if (Equals(field, value)) return;
        field = value;
        _subject.OnNext(this);
    }
}

public sealed class BleMockFactory : IPlatformSpecificBleFactory
{
    private readonly BleBroadcasterMock _broadcaster = new();
    private readonly BlePeripheralMock _peripheral = new();
    public IBleBroadcaster Broadcaster => _broadcaster;
    public IBlePeripheral Peripheral => _peripheral;
    public IEnumerable<IPlatformSpecificBleDevice> EnumerateDevices()
    {
        yield return new MockBleDevice(_broadcaster, _peripheral);
    }
}