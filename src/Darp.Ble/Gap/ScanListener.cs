using System.Threading.Channels;
using Darp.Ble.Gap;

namespace Darp.Ble.Gap;

/// <summary> The queue of one scan. Written by the radio scan without waiting, read by the app's loop </summary>
internal sealed class ScanListener
{
    private readonly Channel<IGapAdvertisement> _channel;
    private readonly ScanDropCounter _dropCounter;
    private readonly int _capacity;

    public ScanListener(QueueOptions queue, ScanDropCounter dropCounter)
    {
        _dropCounter = dropCounter;
        _capacity = queue.Capacity ?? int.MaxValue;
        _channel = queue.Capacity is { } capacity
            ? Channel.CreateBounded<IGapAdvertisement>(
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
                // Runs inside TryWrite on the driver's read loop: only counts
                _ => dropCounter.Increment()
            )
            : Channel.CreateUnbounded<IGapAdvertisement>(new UnboundedChannelOptions { SingleReader = true });
    }

    public ChannelReader<IGapAdvertisement> Reader => _channel.Reader;

    /// <summary> Adds a report. Returns false when this scan is over and should be detached </summary>
    public bool TryWrite(IGapAdvertisement advertisement)
    {
        if (_channel.Writer.TryWrite(advertisement))
            return true;
        // Not written: either the queue is full in Fail mode, or the scan already ended.
        // TryComplete only succeeds in the first case
        if (_channel.Writer.TryComplete(new BleQueueOverflowException(_capacity)))
            _dropCounter.Increment();
        return false;
    }

    /// <summary> Ends the scan after its queued reports, with an error or normally </summary>
    public void Complete(Exception? error = null) => _channel.Writer.TryComplete(error);
}
