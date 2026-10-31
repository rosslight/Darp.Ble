using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> A received advertisement; immutable, so every scan shares it </summary>
public sealed class GapAdvertisement : IGapAdvertisement
{
    private GapAdvertisement(
        DateTimeOffset timestamp,
        BleEventType eventType,
        BleAddress address,
        Physical primaryPhy,
        Physical secondaryPhy,
        AdvertisingSId advertisingSId,
        TxPowerLevel txPower,
        Rssi rssi,
        PeriodicAdvertisingInterval periodicAdvertisingInterval,
        BleAddress directAddress,
        AdvertisingData data
    )
    {
        Timestamp = timestamp;
        EventType = eventType;
        Address = address;
        PrimaryPhy = primaryPhy;
        SecondaryPhy = secondaryPhy;
        AdvertisingSId = advertisingSId;
        TxPower = txPower;
        Rssi = rssi;
        PeriodicAdvertisingInterval = periodicAdvertisingInterval;
        DirectAddress = directAddress;
        Data = data;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public BleEventType EventType { get; }

    /// <inheritdoc />
    public BleAddress Address { get; }

    /// <inheritdoc />
    public Physical PrimaryPhy { get; }

    /// <inheritdoc />
    public Physical SecondaryPhy { get; }

    /// <inheritdoc />
    public AdvertisingSId AdvertisingSId { get; }

    /// <inheritdoc />
    public TxPowerLevel TxPower { get; }

    /// <inheritdoc />
    public Rssi Rssi { get; }

    /// <inheritdoc />
    public PeriodicAdvertisingInterval PeriodicAdvertisingInterval { get; }

    /// <inheritdoc />
    public BleAddress DirectAddress { get; }

    /// <inheritdoc />
    public AdvertisingData Data { get; }

    /// <summary> Create an advertisement; backends report what they received with it </summary>
    /// <param name="timestamp"> The time the advertisement was received </param>
    /// <param name="eventType"> The type of the advertising event </param>
    /// <param name="address"> The address of the advertising device </param>
    /// <param name="primaryPhy"> The primary physical the advertising device used </param>
    /// <param name="secondaryPhy"> The secondary physical the advertising device used </param>
    /// <param name="advertisingSId"> The Advertising SID subfield of the ADI field </param>
    /// <param name="txPower"> The Tx power of the advertising device </param>
    /// <param name="rssi"> The Rssi of the advertising device </param>
    /// <param name="periodicAdvertisingInterval"> The interval of periodic advertisements </param>
    /// <param name="directAddress"> The address of the device the advertisement is directed to </param>
    /// <param name="data"> The data sections of the whole advertisement </param>
    /// <returns> The advertisement </returns>
    [Experimental(Experiments.Backends)]
    public static GapAdvertisement Create(
        DateTimeOffset timestamp,
        BleEventType eventType,
        BleAddress address,
        Physical primaryPhy,
        Physical secondaryPhy,
        AdvertisingSId advertisingSId,
        TxPowerLevel txPower,
        Rssi rssi,
        PeriodicAdvertisingInterval periodicAdvertisingInterval,
        BleAddress directAddress,
        AdvertisingData data
    )
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(directAddress);
        ArgumentNullException.ThrowIfNull(data);
        return new GapAdvertisement(
            timestamp,
            eventType,
            address,
            primaryPhy,
            secondaryPhy,
            advertisingSId,
            txPower,
            rssi,
            periodicAdvertisingInterval,
            directAddress,
            data
        );
    }

    private bool Equals(GapAdvertisement other) =>
        Timestamp.Equals(other.Timestamp)
        && EventType == other.EventType
        && Address.Equals(other.Address)
        && PrimaryPhy == other.PrimaryPhy
        && SecondaryPhy == other.SecondaryPhy
        && AdvertisingSId == other.AdvertisingSId
        && TxPower == other.TxPower
        && Rssi == other.Rssi
        && PeriodicAdvertisingInterval == other.PeriodicAdvertisingInterval
        && DirectAddress.Equals(other.DirectAddress)
        && Data.AsReadOnlyMemory().Span.SequenceEqual(other.Data.AsReadOnlyMemory().Span);

    /// <inheritdoc />
    public bool Equals(IGapAdvertisement? other) =>
        ReferenceEquals(this, other) || (other is GapAdvertisement otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GapAdvertisement otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // A subset of what Equals compares: the timestamp, the sender and the data
        var hash = new HashCode();
        hash.Add(Timestamp);
        hash.Add(Address);
        hash.AddBytes(Data.AsReadOnlyMemory().Span);
        return hash.ToHashCode();
    }
}

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
    public BleEventType EventType => _advertisement.EventType;

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
    public AdvertisingData Data => _advertisement.Data;

    /// <inheritdoc />
    public TUserData UserData { get; }
    object? IGapAdvertisementWithUserData.UserData => UserData;

    private bool Equals(GapAdvertisement<TUserData> other) =>
        _advertisement.Equals(other._advertisement)
        && EqualityComparer<TUserData>.Default.Equals(UserData, other.UserData);

    /// <inheritdoc />
    public bool Equals(IGapAdvertisement? other) =>
        ReferenceEquals(this, other) || (other is GapAdvertisement<TUserData> otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GapAdvertisement<TUserData> otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_advertisement, UserData);
}
