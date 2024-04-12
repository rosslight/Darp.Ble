using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

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