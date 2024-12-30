using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_Disconnect command is used to terminate an existing connection.
/// Produces a <see cref="HciCommandStatusEvent"/> and a <see cref="HciDisconnectionCompleteEvent"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-a211724f-97dc-d1f7-2c28-240854fb271c </remarks>
[BinaryObject]
public readonly partial record struct HciDisconnectCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_Disconnect;

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }
    /// <summary> Indicates the reason for ending the connection </summary>
    /// <remarks> <see cref="HciCommandStatus.AuthenticationFailure"/>
    /// or <see cref="HciCommandStatus.RemoteUserTerminatedConnection"/>
    /// or <see cref="HciCommandStatus.RemoteDeviceTerminatedConnectionDueToLowResources"/>
    /// or <see cref="HciCommandStatus.RemoteDeviceTerminatedConnectionDueToPowerOff"/>
    /// or <see cref="HciCommandStatus.UnsupportedRemoteFeature"/>
    /// or <see cref="HciCommandStatus.PairingWithUnitKeyNotSupported"/>
    /// or <see cref="HciCommandStatus.UnacceptableConnectionParameters"/> </remarks>
    public required HciCommandStatus Reason { get; init; }
}