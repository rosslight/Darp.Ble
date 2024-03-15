using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> The advertisement definition </summary>
public interface IGapAdvertisement
{
    /// <summary> The Timestamp when the report was received </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary> The type of the advertising event </summary>
    PduEventType EventType { get; }
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
    GapAdvertisingData Data { get; }

    /// <summary> Gives back the underlying byte array representing the advertising report </summary>
    /// <returns></returns>
    byte[] AsByteArray();
    //IObservable<IGattPeripheral> Connect(ScannerConfiguration? scannerConfig, ConnectionConfiguration? connectionConfig);
}

/// <summary> An advertisement with attached user data </summary>
/// <typeparam name="TUserData"> The user data </typeparam>
public interface IGapAdvertisement<out TUserData> : IGapAdvertisement
{
    /// <summary> The data specified by the user and attached to the advertisement </summary>
    TUserData UserData { get; }
}