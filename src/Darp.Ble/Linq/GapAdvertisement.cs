using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Linq;

/// <summary> An advertisement with additional data attached </summary>
/// <typeparam name="TUserData"> The type of the attached data </typeparam>
public sealed class GapAdvertisement<TUserData> : IGapAdvertisement<TUserData>
{
    private readonly IGapAdvertisement _advertisement;

    /// <summary> Attach additional data to the advertisement </summary>
    /// <param name="advertisement"> The advertisement to be attached to </param>
    /// <param name="userData"> The data to be attached </param>
    public GapAdvertisement(IGapAdvertisement advertisement, TUserData userData)
    {
        UserData = userData;
        _advertisement = advertisement;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp => _advertisement.Timestamp;
    /// <inheritdoc />
    public PduEventType EventType => _advertisement.EventType;
    /// <inheritdoc />
    public BleAddress Address => _advertisement.Address;
    /// <inheritdoc />
    public Physical PrimaryPhy => _advertisement.PrimaryPhy;
    /// <inheritdoc />
    public Physical SecondaryPhy => _advertisement.SecondaryPhy;
    /// <inheritdoc />
    public AdvertisingSId AdvertisingSId => _advertisement.AdvertisingSId;
    /// <inheritdoc />
    public TxPowerLevel TxPower => _advertisement.TxPower;
    /// <inheritdoc />
    public Rssi Rssi => _advertisement.Rssi;
    /// <inheritdoc />
    public PeriodicAdvertisingInterval PeriodicAdvertisingInterval => _advertisement.PeriodicAdvertisingInterval;
    /// <inheritdoc />
    public BleAddress DirectAddress => _advertisement.DirectAddress;
    /// <inheritdoc />
    public GapAdvertisingData Data => _advertisement.Data;
    /// <inheritdoc />
    public TUserData UserData { get; }
    object? IGapAdvertisementWithUserData.UserData => UserData;

    /// <inheritdoc />
    public byte[] AsByteArray() => _advertisement.AsByteArray();
}