using Android.Bluetooth;
using Darp.Ble.Android.Gap;
using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Android;

/// <summary> The opened adapter; it scans, the other roles are not ported yet </summary>
internal sealed class AndroidAdapterDriver(BluetoothAdapter adapter, TimeProvider timeProvider) : IBleAdapterDriver
{
    private readonly AndroidObserverDriver _observer = new(adapter, timeProvider);

    public Capabilities Capabilities => Capabilities.Observer;

    public IObserverDriver? Observer => _observer;

    public IBroadcasterDriver? Broadcaster => null;

    public ICentralDriver? Central => null;

    public IPeripheralDriver? Peripheral => null;

    /// <summary> Android does not tell apps the adapter's address </summary>
    public BleAddress RandomAddress => BleAddress.NotAvailable;

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Android chooses the adapter's addresses itself");

    public ValueTask DisposeAsync() => new(_observer.StopScanAsync(CancellationToken.None));
}
