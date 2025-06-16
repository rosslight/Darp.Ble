namespace Darp.Ble.Hci.Payload.Result;

/// <summary> A marker interface to find results that provide a command status </summary>
public interface ICommandStatusResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    HciCommandStatus Status { get; }
}
