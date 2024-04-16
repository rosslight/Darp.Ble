using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public abstract class GattServerService(BleUuid uuid) : IGattServerService
{
    private readonly Dictionary<BleUuid, IGattServerCharacteristic> _characteristics = new();
    public IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics => _characteristics;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    protected abstract IObservable<IGattServerCharacteristic> DiscoverCharacteristicsAsyncCore();
    protected abstract IObservable<IGattServerCharacteristic> DiscoverCharacteristicAsyncCore(BleUuid uuid);

    public async Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattServerCharacteristic characteristic = await DiscoverCharacteristicAsyncCore(uuid)
            .FirstAsync()
            .ToTask(cancellationToken);
        _characteristics[uuid] = characteristic;
        return characteristic;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeCore();
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}