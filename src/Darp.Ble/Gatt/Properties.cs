using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> GattProperty definitions </summary>
public static class Properties
{
    /// <summary> A class which represents the <see cref="GattProperty.Notify"/> </summary>
    public sealed class Notify : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Notify;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Write"/> </summary>
    public sealed class Write : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Write;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Read"/> </summary>
    public sealed class Read : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Read;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Read"/> with a generic type </summary>
    public sealed class Read<T> : IBleProperty where T : unmanaged
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Read;
    }
}