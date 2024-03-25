using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload;

public interface IHciEvent<TEvent> : IDecodable<TEvent>
    where TEvent : IHciEvent<TEvent>
{
    static abstract HciEventCode EventCode { get; }
}