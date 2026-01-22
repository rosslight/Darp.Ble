using System.Globalization;
using Shouldly;

namespace Darp.Ble.Hci.Tests;

internal static class ShouldlyExtensions
{
    public static void ShouldHaveValue<T>(this T e, decimal expected)
        where T : unmanaged, Enum, IConvertible
    {
        var enumValue = Convert.ToDecimal(e, CultureInfo.InvariantCulture);
        enumValue.ShouldBe(expected);
    }

    public static void ShouldAllSatisfy<T>(this IEnumerable<T> enums, Action<T> action)
    {
        foreach (T t in enums)
            action(t);
    }
}
