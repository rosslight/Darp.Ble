namespace Darp.Ble;

/// <summary> How the queue of one stream, e.g. one scan, behaves when the app reads slower than data arrives </summary>
/// <remarks> The library only ever adds items without waiting, so a full queue never slows down the radio </remarks>
public sealed class QueueOptions
{
    /// <summary> Creates bounded queue options </summary>
    /// <param name="capacity"> How many items the queue holds; how long a consumer may stall is capacity divided by the item rate </param>
    /// <param name="fullMode"> What happens when the queue is full </param>
    public QueueOptions(int capacity, QueueFullMode fullMode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        if (!Enum.IsDefined(fullMode))
            throw new ArgumentOutOfRangeException(nameof(fullMode), fullMode, "Unknown full mode");
        Capacity = capacity;
        FullMode = fullMode;
    }

    private QueueOptions() { }

    /// <summary> A queue that never drops and grows instead </summary>
    public static QueueOptions Unbounded { get; } = new();

    /// <summary> How many items the queue holds, or null for an unbounded queue </summary>
    public int? Capacity { get; }

    /// <summary> What happens when a bounded queue is full </summary>
    public QueueFullMode FullMode { get; }
}
