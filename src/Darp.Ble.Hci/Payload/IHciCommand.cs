using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload;

/// <summary> An HCI command </summary>
/// <typeparam name="TSelf"> The  </typeparam>
public interface IHciCommand<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] out TSelf
> : IDefaultEncodable<TSelf>
    where TSelf : unmanaged, IHciCommand<TSelf>
{
    /// <summary> The OpCode of the command </summary>
    static abstract HciOpCode OpCode { get; }
}