using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <summary> The abstract gatt server service implementation </summary>
/// <param name="peer"> The peer this service was discovered from </param>
/// <param name="uuid"> The uuid of the gatt service </param>
/// <param name="type"> The type of the gatt service </param>
/// <param name="logger"> An optional logger </param>
public abstract class GattServerService(
    GattServerPeer peer,
    BleUuid uuid,
    GattServiceType type,
    ILogger<GattServerService> logger
) : IGattServerService
{
    private readonly SortedDictionary<ushort, IGattServerCharacteristic> _characteristics = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IGattServerCharacteristic> Characteristics => _characteristics.Values;

    /// <inheritdoc />
    public IGattServerPeer Peer { get; } = peer;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;

    /// <summary> The logger </summary>
    public ILogger<GattServerService> Logger { get; } = logger;

    /// <summary> The service provider </summary>
    protected IServiceProvider ServiceProvider => Peer.Central.Device.ServiceProvider;

    /// <inheritdoc />
    public async Task DiscoverCharacteristicsAsync(CancellationToken cancellationToken = default)
    {
        await foreach (
            GattServerCharacteristic characteristic in DiscoverCharacteristicsCore()
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            await characteristic.DiscoverDescriptorsAsync(cancellationToken).ConfigureAwait(false);
            _characteristics[characteristic.AttributeHandle] = characteristic;
            Logger.LogTrace(
                "Discovered characteristic {Uuid} on {AttributeHandle} with properties {Properties}",
                characteristic.Uuid,
                characteristic.AttributeHandle,
                characteristic.Properties
            );
        }
    }

    /// <summary> Core implementation to discover all characteristics </summary>
    /// <returns> An observable with all characteristics </returns>
    protected abstract IObservable<GattServerCharacteristic> DiscoverCharacteristicsCore();

    /// <inheritdoc />
    public async Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(
        BleUuid uuid,
        CancellationToken cancellationToken = default
    )
    {
        foreach ((ushort _, IGattServerCharacteristic characteristic) in _characteristics)
        {
            if (characteristic.Uuid == uuid)
                return characteristic;
        }
        IGattServerCharacteristic? characteristicToReturn = null;
        await foreach (
            GattServerCharacteristic characteristic in DiscoverCharacteristicsCore(uuid)
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            await characteristic.DiscoverDescriptorsAsync(cancellationToken).ConfigureAwait(false);
            characteristicToReturn ??= characteristic;
            _characteristics[characteristic.AttributeHandle] = characteristic;
            Logger.LogTrace(
                "Discovered characteristic {Uuid} on {AttributeHandle} with properties {Properties}",
                characteristic.Uuid,
                characteristic.AttributeHandle,
                characteristic.Properties
            );
        }
        return characteristicToReturn ?? throw new Exception($"No characteristic with Uuid {uuid} was discovered");
    }

    /// <summary> Core implementation to discover a characteristic with a given <paramref name="uuid"/> </summary>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <returns> An observable with all characteristics </returns>
    protected abstract IObservable<GattServerCharacteristic> DiscoverCharacteristicsCore(BleUuid uuid);
}
