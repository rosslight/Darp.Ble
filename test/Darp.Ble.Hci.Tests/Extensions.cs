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
}