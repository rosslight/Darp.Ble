using Darp.Ble.Hci.Payload;

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

    public static bool TryEncode<T>(this T encodable, Span<byte> destination)
        where T : IEncodable =>
        encodable.TryEncode(destination);
    public static int GetLength<T>(this T encodable)
        where T : IEncodable =>
        encodable.Length;
}