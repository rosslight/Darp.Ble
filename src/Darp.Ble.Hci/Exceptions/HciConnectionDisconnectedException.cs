using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Exceptions;

/// <summary> Represents an error caused by an ACL connection being disconnected while an operation was in-flight </summary>
public sealed class HciConnectionDisconnectedException : HciException
{
    /// <summary> Initialize a new <see cref="HciConnectionDisconnectedException"/> </summary>
    /// <param name="connectionHandle"> The disconnected connection handle </param>
    /// <param name="operation"> The operation that was interrupted </param>
    /// <param name="disconnectReason"> The reported disconnect reason, if known </param>
    /// <param name="innerException"> The inner exception </param>
    public HciConnectionDisconnectedException(
        ushort connectionHandle,
        string operation,
        HciCommandStatus? disconnectReason = null,
        Exception? innerException = null
    )
        : base(GetMessage(connectionHandle, operation, disconnectReason), innerException)
    {
        ConnectionHandle = connectionHandle;
        Operation = operation;
        DisconnectReason = disconnectReason;
    }

    /// <summary> The disconnected connection handle </summary>
    public ushort ConnectionHandle { get; }

    /// <summary> The operation that was interrupted by the disconnect </summary>
    public string Operation { get; }

    /// <summary> The disconnect reason, if it was reported before the operation failed </summary>
    public HciCommandStatus? DisconnectReason { get; }

    private static string GetMessage(ushort connectionHandle, string operation, HciCommandStatus? disconnectReason)
    {
        string message = $"Connection 0x{connectionHandle:X} was disconnected while performing {operation}";
        if (disconnectReason is { } reason)
            message += $" ({reason})";
        return message + ".";
    }
}
