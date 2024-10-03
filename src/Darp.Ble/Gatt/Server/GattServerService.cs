using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <summary> The abstract gatt server service implementation </summary>
/// <param name="uuid"> The uuid of the gatt service </param>
public abstract class GattServerService(BleUuid uuid, ILogger? logger) : IGattServerService
{
    private readonly Dictionary<BleUuid, IGattServerCharacteristic> _characteristics = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics => _characteristics;
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <summary> The logger </summary>
    public ILogger? Logger { get; } = logger;

    /// <inheritdoc />
    public async Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattServerCharacteristic characteristic = await DiscoverCharacteristicAsyncCore(uuid)
            .FirstAsync()
            .ToTask(cancellationToken);
        _characteristics[uuid] = characteristic;
        return characteristic;
    }

    /// <summary> Core implementation to discover all characteristics </summary>
    /// <returns> An observable with all characteristics </returns>
    protected abstract IObservable<IGattServerCharacteristic> DiscoverCharacteristicsAsyncCore();
    /// <summary> Core implementation to discover a characteristic with a given <see cref="uuid"/> </summary>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <returns> An observable with all characteristics </returns>
    protected abstract IObservable<IGattServerCharacteristic> DiscoverCharacteristicAsyncCore(BleUuid uuid);
}