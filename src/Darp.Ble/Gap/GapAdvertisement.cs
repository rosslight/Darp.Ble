using System.Buffers.Binary;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> The implementation of a ble advertisement </summary>
public sealed class GapAdvertisement : IGapAdvertisement
{
    private readonly byte[] _bytes;
    private readonly BleObserver _bleObserver;

    private GapAdvertisement(byte[] bytes, BleObserver bleObserver)
    {
        _bytes = bytes;
        _bleObserver = bleObserver;
    }

    /// <inheritdoc />
    public required DateTimeOffset Timestamp { get; init; }
    /// <inheritdoc />
    public required BleEventType EventType { get;  init; }
    /// <inheritdoc />
    public required BleAddress Address { get;  init; }
    /// <inheritdoc />
    public required Physical PrimaryPhy { get;  init; }
    /// <inheritdoc />
    public required Physical SecondaryPhy { get;  init; }
    /// <inheritdoc />
    public required AdvertisingSId AdvertisingSId { get;  init; }
    /// <inheritdoc />
    public required TxPowerLevel TxPower { get;  init; }
    /// <inheritdoc />
    public required Rssi Rssi { get;  init; }
    /// <inheritdoc />
    public required PeriodicAdvertisingInterval PeriodicAdvertisingInterval { get;  init; }
    /// <inheritdoc />
    public required BleAddress DirectAddress { get;  init; }
    /// <inheritdoc />
    public required AdvertisingData Data { get;  init; }

    /// <summary> Create an advertisement wrapper from bytes of an extended advertising report </summary>
    /// <remarks> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E, 7.7.65.13 LE Extended Advertising Report event </remarks>
    /// <param name="bleObserver"> The observer which produced this advertisement report </param>
    /// <param name="timestamp"> The timestamp the record was recorded </param>
    /// <param name="bytes"> The bytes of a single extended advertising report </param>
    /// <returns> The advertisement </returns>
    public static GapAdvertisement FromExtendedAdvertisingReport(BleObserver bleObserver,
        DateTimeOffset timestamp,
        byte[] bytes)
    {
        ReadOnlySpan<byte> byteBuffer = bytes;
        ushort eventType = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer);
        byte addressType = byteBuffer[2];
        UInt48 address = UInt48.ReadLittleEndian(byteBuffer[3..]);
        byte primaryPhy = byteBuffer[9];
        byte secondaryPhy = byteBuffer[10];
        byte advertisingSId = byteBuffer[11];
        byte txPower = byteBuffer[12];
        byte rssi = byteBuffer[13];
        ushort periodicAdvertisingInterval = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer[14..]);
        byte directAddressType = byteBuffer[16];
        UInt48 directAddress = UInt48.ReadLittleEndian(byteBuffer[17..]);
        byte dataLength = byteBuffer[23];
        ReadOnlyMemory<byte> data = bytes.AsMemory()[24..(24+dataLength)];

        return new GapAdvertisement(bytes, bleObserver)
        {
            Timestamp = timestamp,
            EventType = (BleEventType)eventType,
            Address = new BleAddress((BleAddressType)addressType, address),
            PrimaryPhy = (Physical)primaryPhy,
            SecondaryPhy = (Physical)secondaryPhy,
            AdvertisingSId = (AdvertisingSId)advertisingSId,
            TxPower = (TxPowerLevel)txPower,
            Rssi = (Rssi)rssi,
            PeriodicAdvertisingInterval = (PeriodicAdvertisingInterval)periodicAdvertisingInterval,
            DirectAddress = new BleAddress((BleAddressType)directAddressType, directAddress),
            Data = AdvertisingData.From(data)
        };
    }

    /// <summary> Create from property values </summary>
    /// <param name="bleObserver"> The observer which produced this advertisement report </param>
    /// <param name="timestamp"> The Timestamp when the report was received </param>
    /// <param name="eventType"> The type of the advertising event </param>
    /// <param name="address"> Settings of the primary physical the advertising device used </param>
    /// <param name="primaryPhy"> Settings of the secondary physical the advertising device used </param>
    /// <param name="secondaryPhy"> Value of the Advertising SID subfield in the ADI field of the PDU or, for scan responses, in the ADI field of the original scannable advertisement </param>
    /// <param name="advertisingSId"> The Tx power of the advertising device </param>
    /// <param name="txPower"> The Tx power of the advertising device </param>
    /// <param name="rssi"> The Rssi of the advertising device </param>
    /// <param name="periodicAdvertisingInterval"> The interval of periodic advertisements </param>
    /// <param name="directAddress"> The address of the device the advertisement is directed to </param>
    /// <param name="advertisingDataSections"> The data sections </param>
    /// <returns> The GapAdvertisement </returns>
    public static GapAdvertisement FromExtendedAdvertisingReport(BleObserver bleObserver,
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
        IReadOnlyList<(AdTypes Section, byte[] Bytes)> advertisingDataSections)
    {
        AdvertisingData advertisingData = AdvertisingData.From(advertisingDataSections);
        return FromExtendedAdvertisingReport(bleObserver, timestamp, eventType, address, primaryPhy, secondaryPhy,
            advertisingSId, txPower, rssi, periodicAdvertisingInterval, directAddress, advertisingData);
    }

    /// <summary> Create from property values </summary>
    /// <param name="bleObserver"> The observer which produced this advertisement report </param>
    /// <param name="timestamp"> The Timestamp when the report was received </param>
    /// <param name="eventType"> The type of the advertising event </param>
    /// <param name="address"> Settings of the primary physical the advertising device used </param>
    /// <param name="primaryPhy"> Settings of the secondary physical the advertising device used </param>
    /// <param name="secondaryPhy"> Value of the Advertising SID subfield in the ADI field of the PDU or, for scan responses, in the ADI field of the original scannable advertisement </param>
    /// <param name="advertisingSId"> The Tx power of the advertising device </param>
    /// <param name="txPower"> The Tx power of the advertising device </param>
    /// <param name="rssi"> The Rssi of the advertising device </param>
    /// <param name="periodicAdvertisingInterval"> The interval of periodic advertisements </param>
    /// <param name="directAddress"> The address of the device the advertisement is directed to </param>
    /// <param name="advertisingData"> The data sections </param>
    /// <returns> The GapAdvertisement </returns>
    public static GapAdvertisement FromExtendedAdvertisingReport(BleObserver bleObserver,
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
        AdvertisingData advertisingData)
    {
        ReadOnlySpan<byte> dataSpan = advertisingData.AsReadOnlyMemory().Span;
        var bytes = new byte[24 + dataSpan.Length];
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
        dataSpan.CopyTo(buffer[24..]);
        return new GapAdvertisement(bytes, bleObserver)
        {
            Timestamp = timestamp,
            EventType = eventType,
            Address = address,
            PrimaryPhy = primaryPhy,
            SecondaryPhy = secondaryPhy,
            AdvertisingSId = advertisingSId,
            TxPower = txPower,
            Rssi = rssi,
            PeriodicAdvertisingInterval = periodicAdvertisingInterval,
            DirectAddress = directAddress,
            Data = advertisingData
        };
    }

    /// <inheritdoc />
    public byte[] AsByteArray() => _bytes;
}

/// <summary> An advertisement with additional data attached </summary>
/// <typeparam name="TUserData"> The type of the attached data </typeparam>
public sealed class GapAdvertisement<TUserData> : IGapAdvertisement<TUserData>, IGapAdvertisementWithUserData
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

    /// <inheritdoc />
    public byte[] AsByteArray() => _advertisement.AsByteArray();
}