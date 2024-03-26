using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci;

public sealed class EncodableByteArray : IEncodable
{
    private readonly byte[] _bytes;
    public EncodableByteArray(byte[] bytes) => _bytes = bytes;

    public int Length => _bytes.Length;
    public bool TryEncode(Span<byte> destination) => _bytes.AsSpan().TryCopyTo(destination);
    public byte[] ToByteArray() => _bytes;
}