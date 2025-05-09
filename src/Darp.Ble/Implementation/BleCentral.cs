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
public abstract class BleCentral(BleDevice device, ILogger<BleCentral> logger) : IBleCentral
{
    private readonly ConcurrentDictionary<BleAddress, IGattServerPeer> _peerDevices = new();
    private readonly BleDevice _device = device;

    /// <summary> The logger </summary>
    protected ILogger<BleCentral> Logger { get; } = logger;

    /// <summary> The service provider </summary>
    protected IServiceProvider ServiceProvider => Device.ServiceProvider;

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;

    /// <inheritdoc />
    public IReadOnlyCollection<IGattServerPeer> PeerDevices => _peerDevices.Values.ToArray();

    /// <inheritdoc />
    public IObservable<IGattServerPeer> ConnectToPeripheral(
        BleAddress address,
        BleConnectionParameters? connectionParameters = null,
        BleObservationParameters? scanParameters = null
    )
    {
        connectionParameters ??= new BleConnectionParameters();
        scanParameters ??= Device.Observer.Parameters;
        return Observable.Create<IGattServerPeer>(async observer =>
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
            await _device.Observer.StopObservingAsync().ConfigureAwait(false);
            return DoAfterConnection(
                    ConnectToPeripheralCore(address, connectionParameters, scanParameters)
                        .Do(peer =>
                        {
                            peer.WhenConnectionStatusChanged.Where(x => x is ConnectionStatus.Disconnected)
                                .Do(_ => Logger.LogTrace("Received disconnection event for Peer {@Peer}", peer))
                                .FirstAsync()
                                .Subscribe(__ => _ = peer.DisposeAsync().AsTask());
                            _peerDevices[peer.Address] = peer;
                        })
                )
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
    /// <param name="observationParameters"> The scan parameters to be used for initial discovery </param>
    /// <returns> An observable notifying when a gatt server was connected </returns>
    protected abstract IObservable<GattServerPeer> ConnectToPeripheralCore(
        BleAddress address,
        BleConnectionParameters connectionParameters,
        BleObservationParameters observationParameters
    );

    /// <summary> Remove a specific peer from the central </summary>
    /// <param name="peer"> The peer to be removed </param>
    /// <returns> True if the element is successfully found and removed; otherwise, false </returns>
    public bool RemovePeer(IGattServerPeer peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        return _peerDevices.TryRemove(peer.Address, out _);
    }

    /// <summary> A method that can be used to clean up all resources. </summary>
    /// <remarks> This method is not glued to the <see cref="IAsyncDisposable"/> interface. All disposes should be done using the  </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        foreach (BleAddress address in _peerDevices.Keys)
        {
            if (_peerDevices.TryGetValue(address, out IGattServerPeer? peer))
            {
                await peer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">
    /// True, when this method was called by the synchronous <see cref="IDisposable.Dispose"/> method;
    /// False if called by the asynchronous <see cref="IAsyncDisposable.DisposeAsync"/> method
    /// </param>
    protected virtual void Dispose(bool disposing) { }
}
