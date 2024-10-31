using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The central view of a ble device </summary>
public abstract class BlePeripheral(BleDevice device, ILogger? logger) : IBlePeripheral, IBleBroadcaster
{
    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;
    private readonly Dictionary<BleUuid, IGattClientService> _services = new();
    private readonly Dictionary<BleAddress, IGattClientPeer> _peerDevices = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientService> Services => _services;
    /// <inheritdoc />
    public IReadOnlyDictionary<BleAddress, IGattClientPeer> PeerDevices => _peerDevices;
    /// <inheritdoc />
    public IBleDevice Device { get; } = device;
    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenConnected => _whenConnected.AsObservable();
    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenDisconnected => _whenConnected.SelectMany(x => x.WhenDisconnected.Select(_ => x));

    /// <inheritdoc />
    public async Task<IGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattClientService service = await AddServiceAsyncCore(uuid, cancellationToken).ConfigureAwait(false);
        _services[service.Uuid] = service;
        return service;
    }

    /// <summary> Core implementation to add a new service </summary>
    /// <param name="uuid"> The uuid of the service to be added </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The newly added service </returns>
    protected abstract Task<IGattClientService> AddServiceAsyncCore(BleUuid uuid, CancellationToken cancellationToken);

    /// <summary> Register a newly connected central </summary>
    /// <param name="clientPeer"> The GattClient peer </param>
    protected void OnConnectedCentral(IGattClientPeer clientPeer)
    {
        ArgumentNullException.ThrowIfNull(clientPeer);
        _whenConnected.OnNext(clientPeer);
        _peerDevices[clientPeer.Address] = clientPeer;
        clientPeer.WhenDisconnected.Subscribe(_ => _peerDevices.Remove(clientPeer.Address));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeCore();
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }

    IDisposable IBleBroadcaster.Advertise(AdvertisingSet advertisingSet) => Device.Broadcaster.Advertise(advertisingSet);
    IDisposable IBleBroadcaster.Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters)
    {
        return Device.Broadcaster.Advertise(source, parameters);
    }
    IDisposable IBleBroadcaster.Advertise(AdvertisingData data, TimeSpan interval, AdvertisingParameters? parameters)
    {
        return Device.Broadcaster.Advertise(data, interval, parameters);
    }

    void IBleBroadcaster.StopAll() => Device.Broadcaster.StopAll();
}