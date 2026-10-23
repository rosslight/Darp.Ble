namespace Darp.Ble.Gatt.Services;

/// <summary> The level a hosted battery service answers reads with </summary>
internal sealed class BatteryLevel(byte value)
{
    private readonly Lock _lock = new();
    private byte _value = value;

    public byte Value
    {
        get
        {
            lock (_lock)
                return _value;
        }
        set
        {
            lock (_lock)
                _value = value;
        }
    }
}
