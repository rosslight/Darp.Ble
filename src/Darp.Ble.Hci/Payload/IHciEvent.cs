using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload;

/// <summary> An HCI Event </summary>
/// <typeparam name="TEvent"> The type of the event </typeparam>
public interface IHciEvent<TEvent> : IBinaryReadable<TEvent>
    where TEvent : IHciEvent<TEvent>
{
    /// <summary> The event code of the given event </summary>
    static abstract HciEventCode EventCode { get; }
}
