using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public abstract class GattServerService(BleUuid uuid) : IGattServerService
{
    private readonly Dictionary<BleUuid, IGattServerCharacteristic> _characteristics = new();
    public IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics => _characteristics;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    protected abstract Task DiscoverCharacteristicsAsyncCore(CancellationToken cancellationToken);
    protected abstract Task<IGattServerCharacteristic?> DiscoverCharacteristicAsyncCore(
        BleUuid uuid, CancellationToken cancellationToken);

    public async Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattServerCharacteristic characteristic = await DiscoverCharacteristicAsyncCore(uuid, cancellationToken) ?? throw new Exception("Upsi");
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