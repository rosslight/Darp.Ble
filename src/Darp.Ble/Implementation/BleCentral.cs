using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The central view of a ble device </summary>
public abstract class BleCentral(BleDevice device, ILogger? logger) : IBleCentral
{
    private readonly ConcurrentDictionary<BleAddress, IGattServerPeer> _peerDevices = new();
    private readonly BleDevice _device = device;

    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;
    /// <inheritdoc />
    public IReadOnlyCollection<IGattServerPeer> PeerDevices => _peerDevices.Values.ToArray();

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
            _device.Observer.StopScan();
            return DoAfterConnection(ConnectToPeripheralCore(address, connectionParameters, scanParameters)
                    .Do(peer =>
                    {
                        peer.WhenConnectionStatusChanged
                            .Where(x => x is ConnectionStatus.Disconnected)
                            .Do(_ => Logger?.LogTrace("Received disconnection event for Peer {@Peer}", peer))
                            .FirstAsync()
                            .Subscribe(__ => _ = peer.DisposeAsync().AsTask());
                        _peerDevices[peer.Address] = peer;
                    }))
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
    protected abstract IObservable<GattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters);

    /// <summary> Remove a specific peer from the central </summary>
    /// <param name="peer"> The peer to be removed </param>
    /// <returns> True if the element is successfully found and removed; otherwise, false </returns>
    public bool RemovePeer(IGattServerPeer peer) => _peerDevices.TryRemove(peer.Address, out _);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (BleAddress address in _peerDevices.Keys)
        {
            if (_peerDevices.TryGetValue(address, out IGattServerPeer? peer))
            {
                await peer.DisposeAsync();
            }
        }
        DisposeCore();
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}