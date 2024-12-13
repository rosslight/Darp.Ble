using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload;

/// <summary> An HCI command </summary>
public interface IHciCommand : IWritable
{
    /// <summary> The OpCode of the command </summary>
    static abstract HciOpCode OpCode { get; }
}