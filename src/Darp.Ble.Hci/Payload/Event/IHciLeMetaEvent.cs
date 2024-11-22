namespace Darp.Ble.Hci.Payload.Event;

/// <summary> An LE Meta Event </summary>
/// <typeparam name="TEvent"> The type of the event </typeparam>
public interface IHciLeMetaEvent<TEvent> : IHciEvent<TEvent> where TEvent : IHciEvent<TEvent>
{
#pragma warning disable CA1033
    static HciEventCode IHciEvent<TEvent>.EventCode => HciEventCode.HCI_LE_Meta;
#pragma warning restore CA1033
    /// <summary> The static <see cref="HciLeMetaSubEventType"/> </summary>
    static abstract HciLeMetaSubEventType SubEventType { get; }
    /// <summary> The instance access to the <see cref="HciLeMetaSubEventType"/> </summary>
    HciLeMetaSubEventType SubEventCode { get; }
}