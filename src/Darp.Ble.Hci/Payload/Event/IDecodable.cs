using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> An interface for an object which can be decoded from a span </summary>
/// <typeparam name="TSelf"></typeparam>
public interface IDecodable<TSelf> where TSelf : IDecodable<TSelf>
{
    /// <summary> Try to decode a source of bytes into a result </summary>
    /// <param name="source"> The source of bytes to decode from </param>
    /// <param name="result"> The result if successful </param>
    /// <param name="bytesDecoded"> The number of bytes used for decoding </param>
    /// <returns> True, when decoding was successful </returns>
    static abstract bool TryDecode(in ReadOnlyMemory<byte> source,
        [NotNullWhen(true)] out TSelf? result,
        out int bytesDecoded);
}