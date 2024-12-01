namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The format of the <see cref="AttFindInformationRsp.InformationData"/> </summary>
public enum AttFindInformationFormat
{
    /// <summary> A list of 1 or more handles with their 16-bit Bluetooth UUIDs </summary>
    HandleAnd16BitUuid = 0x01,
    /// <summary> A list of 1 or more handles with their 128-bit UUIDs </summary>
    HandleAnd128BitUuid = 0x02,
}