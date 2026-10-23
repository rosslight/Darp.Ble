using System.Buffers.Binary;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> An advertising report. Immutable: it owns one copy of the report's bytes, which every scan shares </summary>
public sealed class GapAdvertisement : IGapAdvertisement
{
    private const int HeaderLength = 24;

    private readonly ReadOnlyMemory<byte> _bytes;

    private GapAdvertisement(DateTimeOffset timestamp, ReadOnlyMemory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;
        Timestamp = timestamp;
        _bytes = bytes;
        EventType = (BleEventType)BinaryPrimitives.ReadUInt16LittleEndian(span);
        Address = new BleAddress((BleAddressType)span[2], UInt48.ReadLittleEndian(span[3..]));
        PrimaryPhy = (Physical)span[9];
        SecondaryPhy = (Physical)span[10];
        AdvertisingSId = (AdvertisingSId)span[11];
        TxPower = (TxPowerLevel)span[12];
        Rssi = (Rssi)span[13];
        PeriodicAdvertisingInterval = (PeriodicAdvertisingInterval)BinaryPrimitives.ReadUInt16LittleEndian(span[14..]);
        DirectAddress = new BleAddress((BleAddressType)span[16], UInt48.ReadLittleEndian(span[17..]));
        byte dataLength = span[23];
        Data = AdvertisingData.FromOwned(bytes[HeaderLength..(HeaderLength + dataLength)]);
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

    /// <inheritdoc />
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _bytes;

    /// <summary> Create an advertisement from the bytes of an extended advertising report. The bytes are copied </summary>
    /// <remarks> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E, 7.7.65.13 LE Extended Advertising Report event </remarks>
    /// <param name="timestamp"> The time the report was received </param>
    /// <param name="report"> The bytes of a single extended advertising report </param>
    /// <returns> The advertisement </returns>
    public static GapAdvertisement FromExtendedAdvertisingReport(DateTimeOffset timestamp, ReadOnlySpan<byte> report) =>
        new(timestamp, report.ToArray());

    /// <summary> Create an advertisement from its values </summary>
    /// <param name="timestamp"> The time the report was received </param>
    /// <param name="eventType"> The type of the advertising event </param>
    /// <param name="address"> The address of the advertising device </param>
    /// <param name="primaryPhy"> The primary physical the advertising device used </param>
    /// <param name="secondaryPhy"> The secondary physical the advertising device used </param>
    /// <param name="advertisingSId"> The Advertising SID subfield of the ADI field </param>
    /// <param name="txPower"> The Tx power of the advertising device </param>
    /// <param name="rssi"> The Rssi of the advertising device </param>
    /// <param name="periodicAdvertisingInterval"> The interval of periodic advertisements </param>
    /// <param name="directAddress"> The address of the device the advertisement is directed to </param>
    /// <param name="advertisingData"> The data sections </param>
    /// <returns> The advertisement </returns>
    public static GapAdvertisement FromExtendedAdvertisingReport(
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
        AdvertisingData advertisingData
    )
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(directAddress);
        ArgumentNullException.ThrowIfNull(advertisingData);
        ReadOnlySpan<byte> dataSpan = advertisingData.AsReadOnlyMemory().Span;
        var bytes = new byte[HeaderLength + dataSpan.Length];
        Span<byte> buffer = bytes;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)eventType);
        buffer[2] = (byte)address.Type;
        UInt48.WriteLittleEndian(buffer[3..], address.Value);
        buffer[9] = (byte)primaryPhy;
        buffer[10] = (byte)secondaryPhy;
        buffer[11] = (byte)advertisingSId;
        buffer[12] = (byte)txPower;
        buffer[13] = (byte)rssi;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[14..], (ushort)periodicAdvertisingInterval);
        buffer[16] = (byte)directAddress.Type;
        UInt48.WriteLittleEndian(buffer[17..], directAddress.Value);
        buffer[23] = (byte)dataSpan.Length;
        dataSpan.CopyTo(buffer[HeaderLength..]);
        return new GapAdvertisement(timestamp, bytes);
    }

    private bool Equals(GapAdvertisement other) =>
        Timestamp.Equals(other.Timestamp) && _bytes.Span.SequenceEqual(other._bytes.Span);

    /// <inheritdoc />
    public bool Equals(IGapAdvertisement? other) =>
        ReferenceEquals(this, other) || (other is GapAdvertisement otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GapAdvertisement otherAdv && Equals(otherAdv));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // The same values as Equals: the timestamp and the bytes of the report
        var hash = new HashCode();
        hash.Add(Timestamp);
        hash.AddBytes(_bytes.Span);
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
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _advertisement.AsReadOnlyMemory();

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
