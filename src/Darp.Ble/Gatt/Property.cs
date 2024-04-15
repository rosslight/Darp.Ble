using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

public interface IBleProperty
{
    static abstract GattProperty Property { get; }
}

public static class Property
{
    public sealed class Notify : IBleProperty
    {
        /// <inheritdoc />
        public static GattProperty Property => GattProperty.Notify;
    }

    public sealed class Write : IBleProperty
    {
        /// <inheritdoc />
        public static GattProperty Property => GattProperty.Write;
    }
    public sealed class Read : IBleProperty
    {
        /// <inheritdoc />
        public static GattProperty Property => GattProperty.Read;
    }
    public sealed class Read<T> : IBleProperty where T : unmanaged
    {
        /// <inheritdoc />
        public static GattProperty Property => GattProperty.Read;
    }
}