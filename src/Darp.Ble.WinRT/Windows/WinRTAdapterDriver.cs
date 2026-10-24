using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.WinRT.Connections;
using Darp.Ble.WinRT.Gap;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

/// <summary> The opened default adapter; its roles follow what Windows says the adapter can do </summary>
internal sealed class WinRTAdapterDriver : IBleAdapterDriver
{
    private readonly WinRTObserverDriver _observer;
    private readonly WinRTCentralDriver? _central;
    private readonly WinRTPeripheralDriver? _peripheral;
    private readonly WinRTBroadcasterDriver? _broadcaster;

    public WinRTAdapterDriver(BluetoothAdapter adapter, BleBackendContext context)
    {
        _observer = new WinRTObserverDriver(context.TimeProvider);
        RandomAddress = new BleAddress(BleAddressType.Public, (UInt48)adapter.BluetoothAddress);
        if (adapter.IsCentralRoleSupported)
            _central = new WinRTCentralDriver();
        ILogger logger = context.LoggerFactory.CreateLogger<WinRTAdapterDriver>();
        if (adapter.IsPeripheralRoleSupported)
            _peripheral = new WinRTPeripheralDriver(logger);
        // The peripheral role includes the broadcaster; without it, advertising needs offloading
        if (adapter.IsPeripheralRoleSupported || adapter.IsAdvertisementOffloadSupported)
            _broadcaster = new WinRTBroadcasterDriver(_peripheral, logger);
    }

    /// <summary> Windows sets the address and serves the GAP service itself </summary>
    public Capabilities Features => Capabilities.None;

    public IObserverDriver? Observer => _observer;

    public IBroadcasterDriver? Broadcaster => _broadcaster;

    public ICentralDriver? Central => _central;

    public IPeripheralDriver? Peripheral => _peripheral;

    /// <summary> The adapter's own address; Windows chooses the addresses it advertises and connects with </summary>
    public BleAddress RandomAddress { get; }

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Windows chooses the adapter's addresses itself");

    public async ValueTask DisposeAsync()
    {
        await _observer.DisposeAsync().ConfigureAwait(false);
        _peripheral?.Dispose();
    }
}
