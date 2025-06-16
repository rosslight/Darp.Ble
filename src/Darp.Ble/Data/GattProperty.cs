namespace Darp.Ble.Data;

/// <summary> The gatt property of a characteristic </summary>
[Flags]
public enum GattProperty : byte
{
    /// <summary> The characteristic doesn't have any properties that apply. </summary>
    None = 0b0,

    /// <summary> The characteristic supports broadcasting </summary>
    Broadcast = 0b1,

    /// <summary> The characteristic is readable </summary>
    Read = 0b10,

    /// <summary> The characteristic supports Write Without Response </summary>
    WriteWithoutResponse = 0b100,

    /// <summary> The characteristic is writable </summary>
    Write = 0b1000,

    /// <summary> The characteristic is notifiable (without acknowledgment) </summary>
    Notify = 0b10000, // 0x00000010

    /// <summary> The characteristic is indictable (with acknowledgment) </summary>
    Indicate = 0b100000,

    /// <summary> The characteristic supports signed writes </summary>
    AuthenticatedSignedWrites = 0b1000000,
}
