using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> All parameters of the <see cref="HciNumberOfCompletedPacketsEvent"/> </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-ca477bbf-f878-bda9-4eab-174c458d7a94"/>
[BinaryObject]
public readonly partial record struct HciNumberOfCompletedPackets
{
    /// <summary> The Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> The Num_Completed_Packets </summary>
    public required ushort NumCompletedPackets { get; init; }
}
