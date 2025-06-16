using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The group type response data </summary>
/// <param name="Handle"> The Attribute Handle </param>
/// <param name="EndGroup"> The End Group Handle </param>
/// <param name="Value"> The Attribute Value </param>
/// <typeparam name="TAttributeValue"> The type of the value </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3ca57165-f2ce-1531-4583-95d33d899fff_table-idm13358909789874"/>
public readonly record struct AttGroupTypeData<TAttributeValue>(ushort Handle, ushort EndGroup, TAttributeValue Value)
    : IBinaryObject<AttGroupTypeData<TAttributeValue>>
    where TAttributeValue : unmanaged
{
    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttGroupTypeData<TAttributeValue> value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(
        ReadOnlySpan<byte> source,
        out AttGroupTypeData<TAttributeValue> value,
        out int bytesRead
    )
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 4 + Marshal.SizeOf<TAttributeValue>())
            return false;
        ushort handle = BinaryPrimitives.ReadUInt16LittleEndian(source);
        ushort endGroup = BinaryPrimitives.ReadUInt16LittleEndian(source[2..]);
        var attributeValue = MemoryMarshal.Read<TAttributeValue>(source[4..]);
        value = new AttGroupTypeData<TAttributeValue>(handle, endGroup, attributeValue);
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttGroupTypeData<TAttributeValue> value) =>
        TryReadBigEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadBigEndian(
        ReadOnlySpan<byte> source,
        out AttGroupTypeData<TAttributeValue> value,
        out int bytesRead
    )
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public int GetByteCount() => 4 + Marshal.SizeOf<TAttributeValue>();

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination) => TryWriteLittleEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        var valueLength = Marshal.SizeOf<TAttributeValue>();
        if (destination.Length < 4 + valueLength)
            return false;
        BinaryPrimitives.WriteUInt16LittleEndian(destination, Handle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], EndGroup);
        MemoryMarshal.Write(destination[4..], Value);
        bytesWritten += 4 + valueLength;
        return true;
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination) => TryWriteBigEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotSupportedException();
}
