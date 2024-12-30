using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <summary> The gatt server peer </summary>
public abstract class GattServerPeer : IGattServerPeer
{
    private readonly BleCentral _central;
    private readonly Dictionary<BleUuid, IGattServerService> _services = new();
    private bool _isDisposing;

    /// <summary> The behavior subject where the implementation can write to </summary>
    protected BehaviorSubject<ConnectionStatus> ConnectionSubject { get; } = new(ConnectionStatus.Connected);

    /// <summary> The gatt server peer </summary>
    /// <param name="central"> The central that initiated the connection </param>
    /// <param name="address"> The address of the service </param>
    /// <param name="logger"> An optional logger </param>
    protected GattServerPeer(BleCentral central, BleAddress address, ILogger? logger)
    {
        _central = central;
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
    public bool IsConnected => ConnectionSubject.Value is ConnectionStatus.Connected;
    /// <inheritdoc />
    public IObservable<ConnectionStatus> WhenConnectionStatusChanged => ConnectionSubject.AsObservable();

    /// <inheritdoc />
    public async Task DiscoverServicesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposing, this);
        await foreach (IGattServerService service in DiscoverServicesCore()
                           .ToAsyncEnumerable()
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            _services[service.Uuid] = service;
        }
    }

    /// <inheritdoc />
    public async Task<IGattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposing, this);
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
    protected abstract IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposing)
        {
            return;
        }
        _isDisposing = true;
        DisposeCore();
        await DisposeAsyncCore().ConfigureAwait(false);
        Logger?.LogBleServerPeerDisposed(Address);
        _central.RemovePeer(this);
        Debug.Assert(ConnectionSubject.Value is ConnectionStatus.Disconnected, "Disposing of connection subject even though it is not in completed state");
        ConnectionSubject.OnCompleted();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected internal virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected internal virtual void DisposeCore() { }
}