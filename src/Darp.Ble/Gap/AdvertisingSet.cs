using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> An advertising set </summary>
public sealed class AdvertisingSet : IDisposable
{
    private readonly BehaviorSubject<AdvertisingSet> _subject;
    private AdvertisingData? _scanResponseData;
    private AdvertisingData _data;

    /// <summary> Instantiate a new advertising set </summary>
    /// <param name="setId"> The ID of the set </param>
    /// <param name="interval"> The scan interval </param>
    public AdvertisingSet(int setId, ScanTiming interval)
    {
        _subject = new BehaviorSubject<AdvertisingSet>(this);
        _data = AdvertisingData.Empty;
        SetId = setId;
        Interval = interval;
    }

    /// <summary> The ID of the set </summary>
    public int SetId { get; }

    /// <summary> The scan interval </summary>
    public ScanTiming Interval { get; init; }
    /// <summary> The data </summary>
    public AdvertisingData Data { get => _data; [MemberNotNull(nameof(_data))] set => SetAndNotifyIfChanged(ref _data, value); }
    /// <summary> The scan response data </summary>
    public AdvertisingData? ScanResponseData { get => _scanResponseData; set => SetAndNotifyIfChanged(ref _scanResponseData, value); }

    /// <summary> Subscribe to changes </summary>
    public IObservable<AdvertisingSet> WhenChanged => _subject.AsObservable();

    private void SetAndNotifyIfChanged<T>([NotNullIfNotNull(nameof(value))] ref T field, T value)
    {
        if (Equals(field, value)) return;
        field = value;
        _subject.OnNext(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subject.Dispose();
    }
}