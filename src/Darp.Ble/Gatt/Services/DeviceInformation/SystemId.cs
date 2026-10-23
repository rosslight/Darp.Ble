using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Darp.Ble.Gatt.Services;

/// <summary> The system ID of the Device Information service: a 40-bit manufacturer-defined identifier and a 24-bit OUI </summary>
/// <param name="ManufacturerDefinedIdentifier"> The lower 40 bits </param>
/// <param name="OrganizationallyUniqueIdentifier"> The upper 24 bits </param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SystemId(ulong ManufacturerDefinedIdentifier, uint OrganizationallyUniqueIdentifier)
{
    /// <summary> Decodes a system ID, little endian </summary>
    /// <param name="source"> The 8 bytes </param>
    /// <returns> The system ID </returns>
    /// <exception cref="GattException"> The value does not have 8 bytes (InvalidAttributeValueLength) </exception>
    public static SystemId ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        if (source.Length != 8)
        {
            throw new GattException(GattProtocolStatus.InvalidAttributeValueLength, "A system ID has 8 bytes");
        }
        ulong systemId = BinaryPrimitives.ReadUInt64LittleEndian(source);
        return new SystemId(systemId & 0xFF_FFFF_FFFF, (uint)(systemId >> 40));
    }

    /// <summary> Encodes the system ID, little endian </summary>
    /// <returns> The 8 bytes </returns>
    public byte[] ToByteArrayLittleEndian()
    {
        var buffer = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(
            buffer,
            ((ulong)OrganizationallyUniqueIdentifier << 40) | (ManufacturerDefinedIdentifier & 0xFF_FFFF_FFFF)
        );
        return buffer;
    }
}
