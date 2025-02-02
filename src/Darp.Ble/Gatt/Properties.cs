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

    /// <summary> A class which represents the <see cref="GattProperty.Indicate"/> </summary>
    public sealed class Indicate : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Indicate;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Write"/> </summary>
    public sealed class Write : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Write;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Write"/> </summary>
    public sealed class WriteWithoutResponse : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.WriteWithoutResponse;
    }

    /// <summary> A class which represents the <see cref="GattProperty.Read"/> </summary>
    public sealed class Read : IBleProperty
    {
        /// <inheritdoc />
        static GattProperty IBleProperty.GattProperty => GattProperty.Read;
    }
}
