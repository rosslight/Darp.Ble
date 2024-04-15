using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Logger;

namespace Darp.Ble;

public interface IBleCentral
{
    /// <summary> Connect to remote peripheral </summary>
    /// <param name="address"> The address to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    IObservable<IGattServerPeer> ConnectToPeripheral(BleAddress address, BleConnectionParameters? connectionParameters,
        BleScanParameters? scanParameters);
}

/// <summary> The central view of a ble device </summary>
public abstract class BleCentral(BleDevice device, IObserver<LogEvent>? logger) : IBleCentral
{
    private readonly IObserver<LogEvent>? _logger = logger;

    /// <summary> The ble device </summary>
    public BleDevice Device { get; } = device;

    /// <inheritdoc />
    public IObservable<IGattServerPeer> ConnectToPeripheral(BleAddress address, BleConnectionParameters? connectionParameters, BleScanParameters? scanParameters)
    {
        connectionParameters ??= new BleConnectionParameters();
        scanParameters ??= Device.Observer.Parameters;
        return Observable.Create<IGattServerPeer>(observer =>
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
            return ConnectToPeripheralCore(address, connectionParameters, scanParameters)
                .Subscribe(observer);
        });
    }

    protected abstract IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters);
}