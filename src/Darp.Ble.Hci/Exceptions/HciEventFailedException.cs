using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Exceptions;

/// <summary> Represents an error caused by a specific event packet </summary>
/// <param name="packet"> The event packet that produced the error </param>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class HciEventFailedException(HciEventPacket<HciCommandStatusEvent> packet)
    : HciException(
        $"Got failure response for command {packet.Data.CommandOpCode} with status {packet.Data.Status}"
    );
