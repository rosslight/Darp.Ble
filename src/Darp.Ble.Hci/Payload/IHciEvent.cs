using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload;

/// <summary> An HCI Event </summary>
/// <typeparam name="TEvent"> The type of the event </typeparam>
public interface IHciEvent<TEvent> : ISpanReadable<TEvent>
    where TEvent : IHciEvent<TEvent>
{
    /// <summary> The event code of the given event </summary>
    static abstract HciEventCode EventCode { get; }
}