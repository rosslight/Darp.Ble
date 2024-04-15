using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerService : IAsyncDisposable
{
    BleUuid Uuid { get; }
}