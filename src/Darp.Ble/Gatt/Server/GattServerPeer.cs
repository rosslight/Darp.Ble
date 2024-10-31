using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <summary> The gatt server peer </summary>
public abstract class GattServerPeer : IGattServerPeer
{
    private readonly Dictionary<BleUuid, IGattServerService> _services = new();

    /// <summary> The gatt server peer </summary>
    /// <param name="address"> The address of the service </param>
    /// <param name="logger"> An optional logger </param>
    protected GattServerPeer(BleAddress address, ILogger? logger)
    {
        Logger = logger;
        Address = address;
        Logger?.LogBleServerPeerConnected(address);
    }

    /// <summary> The logger </summary>
    protected ILogger? Logger { get; }
    /// <inheritdoc />
    public BleAddress Address { get; }
    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattServerService> Services => _services;
    /// <inheritdoc />
    public abstract IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }

    /// <inheritdoc />
    public async Task DiscoverServicesAsync(CancellationToken cancellationToken = default)
    {
        await foreach (IGattServerService service in DiscoverServicesCore()
                           .ToAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            _services[service.Uuid] = service;
        }
    }

    /// <inheritdoc />
    public async Task<IGattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattServerService service = await DiscoverServiceCore(uuid)
            .FirstAsync()
            .ToTask(cancellationToken)
            .ConfigureAwait(false);

        _services[service.Uuid] = service;
        return service;
    }

    /// <summary> Core implementation to discover services </summary>
    /// <returns> An observable with all discovered services </returns>
    protected internal abstract IObservable<IGattServerService> DiscoverServicesCore();
    /// <summary> Core implementation to discover a specific service </summary>
    /// <param name="uuid"> The uuid of the service </param>
    /// <returns> An observable with the discovered service </returns>
    protected internal abstract IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeCore();
        await DisposeAsyncCore();
        Logger?.LogBleServerPeerDisposed(Address);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected internal virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected internal virtual void DisposeCore() { }
}