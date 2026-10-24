using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> The notifications of one characteristic from the moment it was subscribed, in its own queue (D7) </summary>
/// <remarks>
/// Enumerate it directly; enumerations one after another continue on the same queue, two at the same time throw.
/// Ending an enumeration keeps the subscription; disposing drops it. The stream ends with its connection:
/// normally when the app ended the connection, with a <see cref="BleDisconnectedException"/> when the link was lost.
/// It also ends, with an <see cref="InvalidOperationException"/>, when notifications are disabled or enabling them failed
/// </remarks>
/// <typeparam name="T"> The type of the values; typed characteristics decode while the stream is read </typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "A stream of notifications in the sense of an async stream, not a System.IO.Stream"
)]
public sealed class NotificationStream<T> : IAsyncEnumerable<T>, IDisposable
{
    private readonly NotificationChannel _channel;
    private readonly ItemQueue<byte[]> _queue;
    private readonly Func<byte[], T> _decode;
    private readonly Lock _lock = new();
    private bool _isEnumerating;

    internal NotificationStream(NotificationChannel channel, ItemQueue<byte[]> queue, Func<byte[], T> decode)
    {
        _channel = channel;
        _queue = queue;
        _decode = decode;
    }

    /// <summary> Enumerates the notifications </summary>
    /// <param name="cancellationToken"> Ends this enumeration; the subscription stays </param>
    /// <returns> The enumerator </returns>
    /// <exception cref="InvalidOperationException"> Notifications are neither on nor being enabled, or the stream is already enumerated </exception>
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isEnumerating)
                throw new InvalidOperationException("The notification stream is already being enumerated");
            _isEnumerating = true;
        }
        try
        {
            _channel.EnsureCanListen(_queue);
            while (await _queue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_queue.Reader.TryRead(out byte[]? value))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return _decode(value);
                }
            }
        }
        finally
        {
            lock (_lock)
                _isEnumerating = false;
        }
    }

    /// <summary> Drops the subscription and its queue </summary>
    public void Dispose()
    {
        _channel.Remove(_queue);
        _queue.Complete();
    }
}
