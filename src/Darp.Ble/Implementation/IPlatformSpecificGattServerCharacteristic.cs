using Darp.Ble.Data;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattServerCharacteristic
{
    BleUuid Uuid { get; }
    Task WriteAsync(byte[] bytes, CancellationToken cancellationToken);
}