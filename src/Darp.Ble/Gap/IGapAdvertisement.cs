using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> The advertisement definition </summary>
public interface IGapAdvertisement : IEquatable<IGapAdvertisement>
{
    /// <summary> The Timestamp when the report was received </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary> The type of the advertising event </summary>
    BleEventType EventType { get; }

    /// <summary> The address of the advertising device </summary>
    BleAddress Address { get; }

    /// <summary> Settings of the primary physical the advertising device used </summary>
    Physical PrimaryPhy { get; }

    /// <summary> Settings of the secondary physical the advertising device used </summary>
    Physical SecondaryPhy { get; }

    /// <summary>
    /// Value of the Advertising SID subfield in the ADI field of the PDU or,
    /// for scan responses, in the ADI field of the original scannable advertisement </summary>
    AdvertisingSId AdvertisingSId { get; }

    /// <summary> The Tx power of the advertising device </summary>
    TxPowerLevel TxPower { get; }

    /// <summary> The Rssi of the advertising device </summary>
    Rssi Rssi { get; }

    /// <summary> The interval of periodic advertisements </summary>
    PeriodicAdvertisingInterval PeriodicAdvertisingInterval { get; }

    /// <summary> The address of the device the advertisement is directed to </summary>
    BleAddress DirectAddress { get; }

    /// <summary> The data sections </summary>
    AdvertisingData Data { get; }

    /// <summary> The advertising report in the layout of a single report of the HCI LE Extended Advertising Report event </summary>
    /// <remarks>
    /// The bytes start at Event_Type and do not include the event header. They are read-only; every scan shares them, and the call does not copy.
    /// Only the HCI host receives these bytes from the controller.
    /// Other backends, e.g. WinRT or Android, build them from the values the platform reports.
    /// A value the platform does not report holds its "not available" value,
    /// e.g. <see cref="Physical.NotAvailable"/> (0) for both PHYs on Windows, which a controller never reports as primary PHY.
    /// Treat the bytes as a received HCI report only if the advertisement came from the HCI host.
    /// </remarks>
    /// <returns> The bytes of the advertising report </returns>
    ReadOnlyMemory<byte> AsReadOnlyMemory();
}

/// <summary> An advertisement with attached user data </summary>
/// <typeparam name="TUserData"> The user data </typeparam>
public interface IGapAdvertisement<out TUserData> : IGapAdvertisementWithUserData
{
    /// <summary> The data specified by the user and attached to the advertisement </summary>
    new TUserData UserData { get; }
}
