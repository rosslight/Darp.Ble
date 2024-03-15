namespace Darp.Ble.Data;

public enum Physical : byte
{
    /// <summary>
    /// No packets on this advertising physical channel
    /// </summary>
    NotAvailable = 0x0,
    /// <summary>
    /// Physical device which supports 1 Mega symbols per second (1 symbol per bit = 1 Megabit).
    /// This is standard for pre Bluetooth 5.0 devices
    /// </summary>
    Le1M = 0x01,
    /// <summary>
    /// Physical device which supports 2 Mega symbols per second (1 symbol per bit = 2 Megabit).
    /// Higher frequency means faster data transfer on cost of reduced range.
    /// </summary>
    Le2M = 0x02,
    /// <summary>
    /// Physical device, which sends with more symbols per bit (2 / 8 symbols possible).
    /// Higher  range comes on cost of data rate
    /// </summary>
    LeCoded = 0x3
}