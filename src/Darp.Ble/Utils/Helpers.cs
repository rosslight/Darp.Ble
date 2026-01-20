using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Utils;

internal static class Helpers
{
    public static bool TryRemove<T>(T[] array, T item, [NotNullWhen(true)] out T[]? newArray)
    {
        // Check if there is a handler to remove
        int handlerIndex = Array.IndexOf(array, item);
        if (handlerIndex < 0)
        {
            newArray = null;
            return false;
        }

        Span<T> arraySpan = array;
        if (arraySpan.Length == 1)
        {
            newArray = [];
            return true;
        }

        // Remove the handler from the array
        newArray = new T[arraySpan.Length - 1];
        if (handlerIndex > 0)
            arraySpan[..handlerIndex].CopyTo(newArray);
        if (handlerIndex < arraySpan.Length - 1)
            arraySpan[(handlerIndex + 1)..].CopyTo(newArray.AsSpan()[handlerIndex..]);
        return true;
    }
}
