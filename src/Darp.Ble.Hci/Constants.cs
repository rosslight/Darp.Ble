namespace Darp.Ble.Hci;

using HciPacketInfo = (int HeaderLength, int PayloadLengthOffset, int PayloadLengthSize);

internal static class Constants
{
    public const int HciPacketMaxHeaderLength = 4;

    /// <summary> The Maximum Number of bytes the payload may be. Has to be synced with the controller </summary>
    public const int HciPacketMaxPayloadLength = 1000;

    /// <summary> The HciEventPacketInfo </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-5d748f9e-157a-fb06-2469-874a61a5c08c"/>
    public static readonly HciPacketInfo HciEventPacketInfo = (
        HeaderLength: 4,
        PayloadLengthOffset: 1,
        PayloadLengthSize: 1
    );

    /// <summary> The HciEventPacketInfo </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bc4ffa33-44ef-e93c-16c8-14aa99597cfc"/>
    public static readonly HciPacketInfo HciAclPacketInfo = (
        HeaderLength: 3,
        PayloadLengthOffset: 2,
        PayloadLengthSize: 2
    );
}
