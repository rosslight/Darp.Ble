using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Server;

public sealed class GattServerDevice : IAsyncDisposable, IDisposable
{
    private readonly Dictionary<BleUuid, GattServerService> _services = new();
    private readonly IPlatformSpecificGattServerDevice _platformSpecificGattServerDevice;
    private readonly BehaviorSubject<ConnectionStatus> _connectionSubject;

    public GattServerDevice(IPlatformSpecificGattServerDevice platformSpecificGattServerDevice, bool isAlreadyConnected)
    {
        _platformSpecificGattServerDevice = platformSpecificGattServerDevice;
        _connectionSubject = new BehaviorSubject<ConnectionStatus>(isAlreadyConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected);
        platformSpecificGattServerDevice.WhenConnectionStatusChanged.Subscribe(_connectionSubject);
    }

    public IReadOnlyDictionary<BleUuid, GattServerService> Services => _services;

    public bool IsConnected => _connectionSubject.Value is ConnectionStatus.Connected;
    public IObservable<ConnectionStatus> WhenConnectionStatusChanged => _connectionSubject.AsObservable();

    public async Task DiscoverServicesAsync(CancellationToken cancellationToken = default)
    {
        await foreach (IPlatformSpecificGattServerService platformSpecificService in _platformSpecificGattServerDevice
                           .DiscoverServices()
                           .ToAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            var service = new GattServerService(platformSpecificService);
            _services[service.Uuid] = service;
        }
    }

    public async Task<GattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IPlatformSpecificGattServerService platformSpecificService = await _platformSpecificGattServerDevice
            .DiscoverService(uuid)
            .FirstAsync()
            .ToTask(cancellationToken);

        var service = new GattServerService(platformSpecificService);
        _services[service.Uuid] = service;
        return service;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _platformSpecificGattServerDevice.DisposeAsync();
        _connectionSubject.Dispose();
    }

    void IDisposable.Dispose() => _ = DisposeAsync().AsTask();
}