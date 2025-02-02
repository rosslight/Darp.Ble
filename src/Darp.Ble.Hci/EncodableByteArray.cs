using Darp.BinaryObjects;

namespace Darp.Ble.Hci;

/// <summary>
/// A byte array which can be encoded
/// </summary>
/// <param name="bytes"> The bytes of the byte array </param>
public sealed class EncodableByteArray(byte[] bytes) : IBinaryWritable
{
    private readonly byte[] _bytes = bytes;

    /// <inheritdoc />
    public int GetByteCount() => _bytes.Length;

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination)
    {
        return TryWriteLittleEndian(destination, out _);
    }

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = _bytes.Length;
        return _bytes.AsSpan().TryCopyTo(destination);
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination)
    {
        return TryWriteBigEndian(destination, out _);
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = _bytes.Length;
        return _bytes.AsSpan().TryCopyTo(destination);
    }
}