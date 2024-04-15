using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Server;

public sealed class GattServerService(IPlatformSpecificGattServerService platformSpecificService) : IGattServerService
{
    private readonly IPlatformSpecificGattServerService _platformSpecificService = platformSpecificService;
    private readonly Dictionary<BleUuid, GattServerCharacteristic> _characteristics = new();
    public IReadOnlyDictionary<BleUuid, GattServerCharacteristic> Characteristics => _characteristics;
    /// <inheritdoc />
    public BleUuid Uuid => _platformSpecificService.Uuid;

    public async Task<GattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IPlatformSpecificGattServerCharacteristic platformSpecificCharacteristic = await _platformSpecificService
            .DiscoverCharacteristicAsync(uuid, cancellationToken)
            ?? throw new Exception("Upsi");
        var characteristic = new GattServerCharacteristic(platformSpecificCharacteristic);
        _characteristics[uuid] = characteristic;
        return characteristic;
    }
}