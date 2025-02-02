using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The central view of a ble device </summary>
public abstract class BlePeripheral(BleDevice device, ILogger<BlePeripheral> logger)
    : IBlePeripheral
{
    /// <summary> The logger </summary>
    protected ILogger<BlePeripheral> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Device.LoggerFactory;

    private readonly List<IGattClientService> _services = [];
    private readonly Dictionary<BleAddress, IGattClientPeer> _peerDevices = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IGattClientService> Services => _services;

    /// <inheritdoc />
    public IReadOnlyDictionary<BleAddress, IGattClientPeer> PeerDevices => _peerDevices;

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;

    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenConnected => _whenConnected.AsObservable();

    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenDisconnected =>
        _whenConnected.SelectMany(x => x.WhenDisconnected.Select(_ => x));

    /// <inheritdoc />
    public async Task<IGattClientService> AddServiceAsync(
        BleUuid uuid,
        bool isPrimary,
        CancellationToken cancellationToken = default
    )
    {
        IGattClientService service = await AddServiceAsyncCore(uuid, isPrimary, cancellationToken)
            .ConfigureAwait(false);
        _services.Add(service);
        return service;
    }

    /// <summary> Core implementation to add a new service </summary>
    /// <param name="uuid"> The uuid of the service to be added </param>
    /// <param name="isPrimary"> True, if the service is a primary service; False, if secondary </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The newly added service </returns>
    protected abstract Task<IGattClientService> AddServiceAsyncCore(
        BleUuid uuid,
        bool isPrimary,
        CancellationToken cancellationToken
    );

    /// <summary> Register a newly connected central </summary>
    /// <param name="clientPeer"> The GattClient peer </param>
    protected void OnConnectedCentral(IGattClientPeer clientPeer)
    {
        ArgumentNullException.ThrowIfNull(clientPeer);
        _peerDevices[clientPeer.Address] = clientPeer;
        _whenConnected.OnNext(clientPeer);
        clientPeer.WhenDisconnected.Subscribe(_ => _peerDevices.Remove(clientPeer.Address));
    }

    /// <summary> A method that can be used to clean up all resources. </summary>
    /// <remarks> This method is not glued to the <see cref="IAsyncDisposable"/> interface. All disposes should be done using the  </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">
    /// True, when this method was called by the synchronous <see cref="IDisposable.Dispose"/> method;
    /// False if called by the asynchronous <see cref="IAsyncDisposable.DisposeAsync"/> method
    /// </param>
    protected virtual void Dispose(bool disposing) { }
}
