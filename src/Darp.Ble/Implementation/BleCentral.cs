using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The central view of a ble device </summary>
public abstract class BleCentral(BleDevice device, ILogger? logger) : IBleCentral
{
    private readonly Dictionary<BleAddress, IGattServerPeer> _peerDevices = new();
    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;
    /// <inheritdoc />
    public IReadOnlyCollection<IGattServerPeer> PeerDevices => _peerDevices.Values;

    /// <inheritdoc />
    public IObservable<IGattServerPeer> ConnectToPeripheral(BleAddress address,
        BleConnectionParameters? connectionParameters = null,
        BleScanParameters? scanParameters = null)
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
            device.Observer.StopScan();
            return DoAfterConnection(ConnectToPeripheralCore(address, connectionParameters, scanParameters)
                    .Do(peer => _peerDevices[peer.Address] = peer))
                .Subscribe(observer);
        });
    }

    /// <summary>
    /// A core implementation can overwrite this method for additional calls after the connection was established
    /// </summary>
    /// <param name="source"> The source </param>
    /// <returns> The resulting peer observable  </returns>
    protected virtual IObservable<IGattServerPeer> DoAfterConnection(IObservable<IGattServerPeer> source) => source;

    /// <summary> The core implementation of connecting to the peripheral </summary>
    /// <param name="address"> The address to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    /// <returns> An observable notifying when a gatt server was connected </returns>
    protected abstract IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeCore();
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}