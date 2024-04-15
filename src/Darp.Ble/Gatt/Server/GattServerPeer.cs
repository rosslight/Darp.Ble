using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerPeer : IAsyncDisposable;

public abstract class GattServerPeer : IGattServerPeer
{
    private readonly Dictionary<BleUuid, IGattServerService> _services = new();

    public IReadOnlyDictionary<BleUuid, IGattServerService> Services => _services;
    public abstract IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }

    public async Task DiscoverServicesAsync(CancellationToken cancellationToken)
    {
        await foreach (IGattServerService service in DiscoverServicesInternal()
                           .ToAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            _services[service.Uuid] = service;
        }
    }

    public async Task<IGattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken)
    {
        IGattServerService service = await DiscoverServiceInternal(uuid)
            .FirstAsync()
            .ToTask(cancellationToken);

        _services[service.Uuid] = service;
        return service;
    }

    protected abstract IObservable<IGattServerService> DiscoverServicesInternal();
    protected abstract IObservable<IGattServerService> DiscoverServiceInternal(BleUuid uuid);

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