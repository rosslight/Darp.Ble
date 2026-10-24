using System.Collections.Immutable;
using Darp.Utils.Messaging;

namespace Darp.Ble.Hci;

/// <summary>
/// The subscribers of a message source. Subscribing, unsubscribing and publishing are safe from any thread:
/// requests subscribe and unsubscribe on their own threads while the transport's read loop publishes
/// </summary>
internal sealed class MessageSinks
{
    private ImmutableArray<IMessageSink> _sinks = [];

    public IDisposable Subscribe(IMessageSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ImmutableInterlocked.Update(ref _sinks, static (sinks, added) => sinks.Add(added), sink);
        return FuncDisposable.Create(
            (Self: this, Sink: sink),
            static state =>
                ImmutableInterlocked.Update(
                    ref state.Self._sinks,
                    static (sinks, removed) => sinks.Remove(removed),
                    state.Sink
                )
        );
    }

    public void Publish<T>(in T message)
        where T : allows ref struct
    {
        foreach (IMessageSink sink in _sinks)
        {
            if (sink is IMessageSink<T> receiver)
                receiver.Publish(message);
            else if (sink is IAnyMessageSink anyReceiver)
                anyReceiver.Publish(message);
        }
    }
}
