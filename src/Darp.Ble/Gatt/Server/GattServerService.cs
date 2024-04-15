using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public abstract class GattServerService(BleUuid uuid) : IGattServerService
{
    private readonly Dictionary<BleUuid, IGattServerCharacteristic> _characteristics = new();
    public IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics => _characteristics;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    protected abstract Task DiscoverCharacteristicsInternalAsync(CancellationToken cancellationToken);
    protected abstract Task<IGattServerCharacteristic?> DiscoverCharacteristicInternalAsync(
        BleUuid uuid, CancellationToken cancellationToken);

    public async Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattServerCharacteristic characteristic = await DiscoverCharacteristicInternalAsync(uuid, cancellationToken)
                                                   ?? throw new Exception("Upsi");
        _characteristics[uuid] = characteristic;
        return characteristic;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeSyncInternal();
        await DisposeInternalAsync();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeInternalAsync() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeSyncInternal() { }
}