using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

public interface IPlatformSpecificBlePeripheral
{
    Task<IPlatformSpecificGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken);
    IObservable<IGattClientPeer> WhenConnected { get; }
}

public abstract class BlePeripheral(BleDevice device, IObserver<LogEvent>? logger) : IBlePeripheral
{
    private readonly IObserver<LogEvent>? Logger = logger;
    private readonly Dictionary<BleUuid, IGattClientService> _services = new();

    /// <summary> The ble device </summary>
    public BleDevice Device { get; } = device;
    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, IGattClientService> Services => _services;

    /// <inheritdoc />
    public async Task<IGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken)
    {
        IGattClientService service = await AddServiceAsyncCore(uuid, cancellationToken);
        _services[service.Uuid] = service;
        return service;
    }

    protected abstract Task<IGattClientService> AddServiceAsyncCore(BleUuid uuid, CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract IObservable<IGattClientPeer> WhenConnected { get; }

    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenDisconnected => WhenConnected.SelectMany(x => x.WhenDisconnected.Select(_ => x));
}