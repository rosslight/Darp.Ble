namespace Darp.Ble.Data;

/// <summary>
/// The event type of the advertising report
/// </summary>
[Flags]
public enum BleEventType
{
    /// <summary> None </summary>
    None = 0,
    /// <summary> Indicates, that one can connect to the sender of the event </summary>
    Connectable = 0b00001,
    /// <summary> Indicates, that the sender is capable of handling a scan request </summary>
    Scannable = 0b00010,
    /// <summary> Indicates, that the sender only accepts connections from a known peer </summary>
    Directed = 0b00100,
    /// <summary>
    /// This advertisement is a scan response to a scan request issued for a scannable advertisement.
    /// </summary>
    ScanResponse = 0b01000,
    /// <summary> This advertisement was sent using a legacy PDU </summary>
    Legacy = 0b10000,
    /// <summary>
    /// The advertisement is undirected and indicates that the device is not connectable nor scannable.
    /// This advertisement type can carry data.
    /// </summary>
    AdvNonConnInd = Legacy,
    /// <summary>
    /// The advertisement is undirected and indicates that the device is scannable but not connectable.
    /// This advertisement type can carry data.
    /// </summary>
    AdvScanInd = Legacy | Scannable,

    /// <summary>
    /// The advertisement is undirected and indicates that the device is connectable and scannable.
    /// This advertisement type can carry data.
    /// </summary>
    AdvInd = Legacy | Connectable | Scannable,
    /// <summary>
    /// The advertisement is directed and indicates that the device is connectable but not scannable.
    /// This advertisement type cannot carry data.
    /// </summary>
    AdvDirectInd = Legacy | Connectable | Directed
}