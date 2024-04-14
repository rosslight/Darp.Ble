using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> The central view of a ble device </summary>
public sealed class BleCentral
{
    private readonly IPlatformSpecificBleCentral _central;
    private readonly IObserver<LogEvent>? _logger;

    /// <summary> The ble device </summary>
    public BleDevice Device { get; }

    internal BleCentral(BleDevice device, IPlatformSpecificBleCentral central, IObserver<LogEvent>? logger)
    {
        _central = central;
        _logger = logger;
        Device = device;
    }

    /// <summary> Connect to remote peripheral </summary>
    /// <param name="address"> The address to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    public IObservable<GattServerPeer> ConnectToPeripheral(BleAddress address, BleConnectionParameters? connectionParameters, BleScanParameters? scanParameters)
    {
        connectionParameters ??= new BleConnectionParameters();
        scanParameters ??= Device.Observer.Parameters;
        return Observable.Create<GattServerPeer>(observer =>
        {
            if (connectionParameters.ConnectionInterval is < ConnectionTiming.MinValue or > ConnectionTiming.MaxValue)
            {
                observer.OnError(new BleCentralConnectionFailedException(this, "Supplied invalid connectionInterval"));
                return Disposable.Empty;
            }
            if (scanParameters.ScanInterval < ScanTiming.MinValue)
            {
                observer.OnError(new BleCentralConnectionFailedException(this, "Supplied invalid scanInterval"));
                return Disposable.Empty;
            }
            if (scanParameters.ScanWindow < ScanTiming.MinValue)
            {
                observer.OnError(new BleCentralConnectionFailedException(this, "Supplied invalid scanWindow"));
                return Disposable.Empty;
            }
            return _central.ConnectToPeripheral(address, connectionParameters, scanParameters)
                .Subscribe(observer);
        });
    }
}

public interface IGattServerService<TService> : IGattServerService
    where TService : IGattServerService<TService>
{
    static abstract Task<TService> CreateAsync(IPlatformSpecificGattServerService platformSpecificService);
}