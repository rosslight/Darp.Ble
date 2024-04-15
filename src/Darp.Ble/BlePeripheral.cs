using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Logger;

namespace Darp.Ble;

public abstract class BlePeripheral(BleDevice device, IObserver<LogEvent>? logger) : IBlePeripheral
{
    protected IObserver<LogEvent>? Logger { get; } = logger;
    private readonly Dictionary<BleUuid, IGattClientService> _services = new();
    private readonly Dictionary<BleAddress, IGattClientPeer> _peerDevices = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientService> Services => _services;

    public IReadOnlyDictionary<BleAddress, IGattClientPeer> PeerDevices => _peerDevices;

    /// <summary> The ble device </summary>
    public BleDevice Device { get; } = device;

    /// <inheritdoc />
    public async Task<IGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IGattClientService service = await CreateServiceAsyncCore(uuid, cancellationToken);
        _services[service.Uuid] = service;
        return service;
    }

    protected abstract Task<IGattClientService> CreateServiceAsyncCore(BleUuid uuid, CancellationToken cancellationToken);

    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenConnected => _whenConnected.AsObservable();

    protected void OnConnectedCentral(IGattClientPeer clientPeer)
    {
        _whenConnected.OnNext(clientPeer);
        _peerDevices[clientPeer.Address] = clientPeer;
        clientPeer.WhenDisconnected.Subscribe(_ =>
        {
            _peerDevices.Remove(clientPeer.Address);
        });
    }

    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenDisconnected => _whenConnected.SelectMany(x => x.WhenDisconnected.Select(_ => x));
}