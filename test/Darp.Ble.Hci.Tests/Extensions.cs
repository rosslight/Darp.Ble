using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Tests;

public static class Extensions
{
    public static IEnumerable<(T First, T Second)> Pairs<T>(this IEnumerable<T> source)
    {
        T? lastItem = default;
        var isFirstState = true;
        foreach (T item in source)
        {
            if (isFirstState)
            {
                lastItem = item;
                isFirstState = false;
                continue;
            }
            yield return (lastItem!, item);
            isFirstState = true;
        }
    }

    public static IEnumerable<T[]> PairsOf<T>(this IEnumerable<T> source, int numberOfItems)
    {
        T[]? items = null;
        var i = 0;
        foreach (T item in source)
        {
            if (i < numberOfItems - 1)
            {
                items ??= new T[numberOfItems];
                items[i] = item;
                i++;
                continue;
            }
            items![i] = item;
            yield return items;
            items = null;
            i = 0;
        }

        if (i is not 0)
        {
            throw new ArgumentOutOfRangeException(nameof(source), $"Size of enumerable is not of a multiple of {numberOfItems}");
        }
    }

    public static bool TryWriteLittleEndian<T>(this T encodable, Span<byte> destination)
        where T : IBinaryWritable =>
        encodable.TryWriteLittleEndian(destination);
    public static int GetByteCount<T>(this T encodable)
        where T : IBinaryWritable =>
        encodable.GetByteCount();

    public static bool TryReadLittleEndian<T>(ReadOnlySpan<byte> memory, [NotNullWhen(true)] out T? result, out int decoded)
        where T : IBinaryReadable<T> =>
        T.TryReadLittleEndian(memory, out result, out decoded);
}