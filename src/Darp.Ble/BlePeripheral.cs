using System.Reactive.Linq;
using System.Reactive.Subjects;
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

public sealed class BlePeripheral : IBlePeripheral
{
    private readonly IObserver<LogEvent>? _logger;
    private readonly IPlatformSpecificBlePeripheral _peripheral;
    private readonly Subject<IGattClientPeer> _whenDisconnected = new();
    private readonly Dictionary<BleUuid, GattClientService> _services = new();

    /// <summary> The ble device </summary>
    public BleDevice Device { get; }
    /// <inheritdoc />
    public IReadOnlyDictionary<BleUuid, GattClientService> Services => _services;

    internal BlePeripheral(BleDevice device,
        IPlatformSpecificBlePeripheral peripheral,
        IObserver<LogEvent>? logger)
    {
        _peripheral = peripheral;
        WhenDisconnected = peripheral.WhenConnected.SelectMany(x => x.WhenDisconnected.Select(_ => x));
        _logger = logger;
        Device = device;
    }

    /// <inheritdoc />
    public async Task<GattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default)
    {
        IPlatformSpecificGattClientService specificService = await _peripheral.AddServiceAsync(uuid, cancellationToken);
        var service = new GattClientService(uuid, specificService);
        _services[service.Uuid] = service;
        return service;
    }
    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenConnected => _peripheral.WhenConnected;
    /// <inheritdoc />
    public IObservable<IGattClientPeer> WhenDisconnected { get; }
}