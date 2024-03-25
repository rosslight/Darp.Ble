using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload;

public interface IHciCommand<out TSelf> : IDefaultEncodable<TSelf>
    where TSelf : unmanaged, IHciCommand<TSelf>
{
    static abstract HciOpCode OpCode { get; }
}