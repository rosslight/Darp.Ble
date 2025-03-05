using System.Reflection;

namespace Darp.Ble.Tests.TestUtils;

public static class NSubstituteExtensions
{
    public static object? InvokeNonPublicMethod<T>(this T obj, string method, params object[] parameters) =>
        obj.GetNonPublicMethod(method).Invoke(obj, parameters);

    private static MethodInfo GetNonPublicMethod<T>(this T _, string method) =>
        typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new ArgumentNullException(nameof(method));

    public static TProp GetNonPublicProperty<TProp>(this object obj, string propertyName)
    {
        PropertyInfo info =
            obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new ArgumentNullException(nameof(propertyName));
        return (TProp)(info.GetValue(obj) ?? throw new ArgumentNullException(propertyName));
    }
}
