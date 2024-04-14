using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Server;

public sealed class GattServerCharacteristic(IPlatformSpecificGattServerCharacteristic platformSpecificCharacteristic)
{
    private readonly IPlatformSpecificGattServerCharacteristic _platformSpecificCharacteristic = platformSpecificCharacteristic;

    public BleUuid Uuid => _platformSpecificCharacteristic.Uuid;

    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        await _platformSpecificCharacteristic.WriteAsync(bytes, cancellationToken);
    }

    public IObservable<byte[]> OnNotify()
    {
        return Observable.Empty<byte[]>();
    }
}