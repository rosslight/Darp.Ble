using Darp.Ble.Data;

namespace Darp.Ble.Gap;

internal sealed class GapAdvertisementWithScanResponse(IGapAdvertisement advertisement, IGapAdvertisement scanResponse)
    : IGapAdvertisementWithScanResponse
{
    private readonly IGapAdvertisement _advertisement = advertisement;
    private readonly IGapAdvertisement _scanResponse = scanResponse;

    IGapAdvertisement IGapAdvertisementWithScanResponse.ScanResponse => _scanResponse;
    public IBleObserver Observer => _advertisement.Observer;
    public DateTimeOffset Timestamp => _advertisement.Timestamp;
    public BleEventType EventType => _advertisement.EventType;
    public BleAddress Address => _advertisement.Address;
    public Physical PrimaryPhy => _advertisement.PrimaryPhy;
    public Physical SecondaryPhy => _advertisement.SecondaryPhy;
    public AdvertisingSId AdvertisingSId => _advertisement.AdvertisingSId;
    public TxPowerLevel TxPower => _advertisement.TxPower;
    public Rssi Rssi => _advertisement.Rssi;
    public PeriodicAdvertisingInterval PeriodicAdvertisingInterval => _advertisement.PeriodicAdvertisingInterval;
    public BleAddress DirectAddress => _advertisement.DirectAddress;
    public AdvertisingData Data { get; } = AdvertisingData.From(advertisement.Data.Concat(scanResponse.Data).ToArray());

    public byte[] AsByteArray() => _advertisement.AsByteArray();

    private bool Equals(GapAdvertisementWithScanResponse other) =>
        _advertisement.Equals(other._advertisement) && _scanResponse.Equals(other._scanResponse);

    public bool Equals(IGapAdvertisement? other) =>
        ReferenceEquals(this, other) || (other is GapAdvertisementWithScanResponse otherAdv && Equals(otherAdv));

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GapAdvertisementWithScanResponse otherAdv && Equals(otherAdv));

    public override int GetHashCode() => HashCode.Combine(_advertisement, _scanResponse);
}
