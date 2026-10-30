using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble;

/// <summary> A stream's queue with <see cref="QueueFullMode.Fail"/> was full, so the stream ended </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Its capacity is required; a constructor with only a message would leave it unset"
)]
public sealed class BleQueueOverflowException : BleException
{
    /// <summary> Initializes an exception for a full queue, e.g. in tests of an app's error handling </summary>
    /// <param name="capacity"> The capacity of the queue that was full </param>
    public BleQueueOverflowException(int capacity)
        : base($"The queue of {capacity} items was full", isTransient: false, nativeCode: null, innerException: null)
    {
        Capacity = capacity;
    }

    /// <summary> The capacity of the queue that was full </summary>
    public int Capacity { get; }
}
