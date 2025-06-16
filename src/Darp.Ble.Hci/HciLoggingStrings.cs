using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Darp.BinaryObjects;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary> String constants for the Darp.Ble.Hci specific logging </summary>
public static class HciLoggingStrings
{
    /// <summary> The name of the Darp.Ble.Hci activity </summary>
    public const string ActivityName = "Darp.Ble.Hci";

    /// <summary> The name of the Darp.Ble.Hci activity </summary>
    public const string TracingActivityName = "Darp.Ble.Hci.Tracing";

    private static IEnumerable<(string, object?)> Deconstruct<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T
    >(string name, T obj, bool orderEntries, bool writeRawBytes = true)
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        for (var index = 0; index < properties.Length; index++)
        {
            PropertyInfo propertyInfo = properties[index];
            object? value = propertyInfo.GetValue(obj);
            value = value switch
            {
                ReadOnlyMemory<sbyte> x => x.ToArray(),
                ReadOnlyMemory<byte> x => x.ToArray(),
                ReadOnlyMemory<short> x => x.ToArray(),
                ReadOnlyMemory<ushort> x => x.ToArray(),
                ReadOnlyMemory<char> x => x.ToArray(),
                ReadOnlyMemory<int> x => x.ToArray(),
                ReadOnlyMemory<uint> x => x.ToArray(),
                ReadOnlyMemory<float> x => x.ToArray(),
                ReadOnlyMemory<long> x => x.ToArray(),
                ReadOnlyMemory<ulong> x => x.ToArray(),
                ReadOnlyMemory<double> x => x.ToArray(),
                _ => value,
            };
            yield return (
                orderEntries ? $"{name}.O{index:00}_{propertyInfo.Name}" : $"{name}.{propertyInfo.Name}",
                value
            );
        }

        if (writeRawBytes && obj is IBinaryWritable writable)
            yield return ($"{name}.RawBytes", writable.ToArrayLittleEndian());
    }

    /// <summary> Deconstruct an object and add it to the activity as tags </summary>
    /// <param name="activity"> The activity to add the tags to </param>
    /// <param name="name"> The name of the object to deconstruct </param>
    /// <param name="obj"> The object </param>
    /// <param name="orderEntries"> If true, append an index; otherwise do not append anything </param>
    /// <param name="writeRawBytes"> If true and T is of <see cref="IBinaryWritable"/>, write the raw bytes; otherwise do nothing </param>
    /// <typeparam name="T"> The type of the object to add </typeparam>
    /// <returns> This, for convenient chaining </returns>
    public static Activity SetDeconstructedTags<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T
    >(this Activity activity, string name, T obj, bool orderEntries, bool writeRawBytes = true)
    {
        ArgumentNullException.ThrowIfNull(activity);

        foreach ((string key, object? value) in Deconstruct(name, obj, orderEntries, writeRawBytes))
            activity.SetTag(key, value);

        return activity;
    }

    /// <summary> Begin a scope if the minimum level is fulfilled </summary>
    /// <param name="logger"> The logger to add the scope to </param>
    /// <param name="minLevel"> The minimum level of the logger to start adding to </param>
    /// <param name="name"> The name of the object to deconstruct </param>
    /// <param name="obj"> The object </param>
    /// <param name="orderEntries"> If true, append an index; otherwise do not append anything </param>
    /// <param name="writeRawBytes"> If true and T is of <see cref="IBinaryWritable"/>, write the raw bytes; otherwise do nothing </param>
    /// <typeparam name="T"> The type of the object to add </typeparam>
    /// <returns> The disposable of the scope </returns>
    public static IDisposable? BeginDeconstructedScope<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T
    >(this ILogger logger, LogLevel minLevel, string name, T obj, bool orderEntries, bool writeRawBytes = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (!logger.IsEnabled(minLevel))
            return null;

        Dictionary<string, object?> dictionary = Deconstruct(name, obj, orderEntries, writeRawBytes)
            .ToDictionary(StringComparer.Ordinal);

        return logger.BeginScope(dictionary);
    }
}
