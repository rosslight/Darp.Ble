using Darp.Ble.Data;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> UUIDs as ATT sends them: 16-bit ones in 2 bytes, every other in 16 (GATT expands 32-bit UUIDs) </summary>
internal static class AttUuid
{
    public static byte[] ToBytes(BleUuid uuid) =>
        uuid.Type is BleUuidType.Uuid16
            ? uuid.ToByteArray()
            : new BleUuid(BleUuidType.Uuid128, uuid.Value).ToByteArray();
}
