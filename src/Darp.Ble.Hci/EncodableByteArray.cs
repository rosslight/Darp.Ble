using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci;

/// <summary>
/// A byte array which can be encoded
/// </summary>
/// <param name="bytes"> The bytes of the byte array </param>
public sealed class EncodableByteArray(byte[] bytes) : IEncodable
{
    private readonly byte[] _bytes = bytes;

    /// <inheritdoc />
    public int Length => _bytes.Length;

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination) => _bytes.AsSpan().TryCopyTo(destination);

    /// <inheritdoc />
    public byte[] ToByteArray() => _bytes;
}