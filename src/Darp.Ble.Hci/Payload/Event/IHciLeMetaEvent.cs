namespace Darp.Ble.Hci.Payload.Event;

public interface IHciLeMetaEvent<TEvent> : IHciEvent<TEvent> where TEvent : IHciEvent<TEvent>
{
    static HciEventCode IHciEvent<TEvent>.EventCode => HciEventCode.HCI_LE_Meta;
    static abstract HciLeMetaSubEventType SubEventType { get; }
    HciLeMetaSubEventType SubEventCode { get; }
}