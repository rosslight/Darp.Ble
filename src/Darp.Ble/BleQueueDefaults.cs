namespace Darp.Ble;

/// <summary> The queue options a <see cref="BleManager"/> uses when a stream does not bring its own </summary>
public sealed class BleQueueDefaults
{
    private QueueOptions _advertisements = new(10_000, QueueFullMode.DropOldest);

    internal BleQueueDefaults() { }

    /// <summary> The queue of each scan; by default 10,000 advertisements, dropping the oldest </summary>
    public QueueOptions Advertisements
    {
        get => _advertisements;
        set => _advertisements = value ?? throw new ArgumentNullException(nameof(value));
    }
}
