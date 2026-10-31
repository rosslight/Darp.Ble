using System.Threading.Channels;

namespace Darp.Ble;

/// <summary> The queue of one stream, e.g. one scan. Written by the driver's thread without waiting, read by the app's loop </summary>
/// <typeparam name="T"> The type of the items </typeparam>
internal sealed class ItemQueue<T>
{
    private readonly Channel<T> _channel;
    private readonly Action _onDrop;
    private readonly int _capacity;
    private int _completed;

    /// <summary> Creates the queue of one stream </summary>
    /// <param name="queue"> The capacity and what happens when the queue is full </param>
    /// <param name="onDrop"> Counts a dropped item; runs on the writer's thread </param>
    public ItemQueue(QueueOptions queue, Action onDrop)
    {
        _onDrop = onDrop;
        _capacity = queue.Capacity ?? int.MaxValue;
        _channel = queue.Capacity is { } capacity
            ? Channel.CreateBounded<T>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = queue.FullMode switch
                    {
                        QueueFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
                        QueueFullMode.DropNewest => BoundedChannelFullMode.DropWrite,
                        _ => BoundedChannelFullMode.Wait,
                    },
                    SingleReader = true,
                },
                _ => onDrop()
            )
            : Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true });
    }

    public ChannelReader<T> Reader => _channel.Reader;

    /// <summary> Whether the stream ended; items queued before may still be read </summary>
    public bool IsCompleted => Volatile.Read(ref _completed) == 1;

    /// <summary> Adds an item. Returns false when this stream is over and should be detached </summary>
    public bool TryWrite(T item)
    {
        if (_channel.Writer.TryWrite(item))
            return true;
        // Not written: either the queue is full in Fail mode, or the stream already ended.
        // Completing only succeeds in the first case
        if (Complete(new BleQueueOverflowException(_capacity)))
            _onDrop();
        return false;
    }

    /// <summary> Ends the stream after its queued items, with an error or normally </summary>
    /// <returns> Whether this call ended it </returns>
    public bool Complete(Exception? error = null)
    {
        if (!_channel.Writer.TryComplete(error))
            return false;
        Volatile.Write(ref _completed, 1);
        return true;
    }
}
