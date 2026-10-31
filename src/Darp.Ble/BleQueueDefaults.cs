namespace Darp.Ble;

/// <summary> The queue options a <see cref="BleManager"/> uses when a stream does not bring its own </summary>
public sealed class BleQueueDefaults
{
    private QueueOptions _advertisements = new(10_000, QueueFullMode.DropOldest);
    private QueueOptions _notifications = new(1_000, QueueFullMode.Fail);

    internal BleQueueDefaults() { }

    /// <summary> The queue of each scan; by default 10,000 advertisements, dropping the oldest </summary>
    public QueueOptions Advertisements
    {
        get => _advertisements;
        set => _advertisements = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary> The queue of each notification stream; by default 1,000 notifications, ending the stream when full </summary>
    /// <remarks> Notifications usually carry state an app must not miss, so a full queue ends the stream instead of dropping </remarks>
    public QueueOptions Notifications
    {
        get => _notifications;
        set => _notifications = value ?? throw new ArgumentNullException(nameof(value));
    }
}
