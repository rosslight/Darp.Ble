namespace Darp.Ble;

/// <summary> What happens when a bounded queue is full </summary>
public enum QueueFullMode
{
    /// <summary> The oldest queued item is dropped to make room </summary>
    DropOldest,

    /// <summary> The incoming item is dropped; the queue keeps what it has </summary>
    DropNewest,

    /// <summary> The stream ends with a <see cref="BleQueueOverflowException"/> after its queued items </summary>
    Fail,
}
