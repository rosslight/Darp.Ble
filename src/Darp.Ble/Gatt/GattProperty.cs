namespace Darp.Ble.Gatt;

public static class GattProperty
{
    public sealed class Notify;
    public sealed class Write;
    public sealed class Read;
    public sealed class Read<T> where T : unmanaged;
}