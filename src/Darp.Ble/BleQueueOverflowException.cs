using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble;

/// <summary> A stream's queue with <see cref="QueueFullMode.Fail"/> was full, so the stream ended </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Thrown by the library only"
)]
public sealed class BleQueueOverflowException : BleException
{
    internal BleQueueOverflowException(int capacity)
        : base($"The queue of {capacity} items was full", isTransient: false, nativeCode: null, innerException: null)
    {
        Capacity = capacity;
    }

    /// <summary> The capacity of the queue that was full </summary>
    public int Capacity { get; }
}
