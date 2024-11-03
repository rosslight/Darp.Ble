namespace Darp.Ble.Hci.Payload;

/// <summary> An interface for an object which can be encoded to a span </summary>
public interface IEncodable
{
    /// <summary> The length of the encoded bytes </summary>
    int Length { get; }
    /// <summary> Encode the object into the span </summary>
    /// <param name="destination"> The span </param>
    /// <returns> True, when encoding was successful </returns>
    bool TryEncode(Span<byte> destination);

    /// <summary> Encode to a byte array </summary>
    /// <returns> The byte array </returns>
    byte[] ToByteArray()
    {
        var bytes = new byte[Length];
        TryEncode(bytes);
        return bytes;
    }
}