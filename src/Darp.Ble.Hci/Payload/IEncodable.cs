namespace Darp.Ble.Hci.Payload;

public interface IEncodable
{
    int Length { get; }
    bool TryEncode(Span<byte> destination);

    byte[] ToByteArray()
    {
        var bytes = new byte[Length];
        TryEncode(bytes);
        return bytes;
    }
}