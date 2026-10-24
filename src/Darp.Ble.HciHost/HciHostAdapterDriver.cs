using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.HciHost.Connections;
using Darp.Ble.HciHost.Gap;

namespace Darp.Ble.HciHost;

/// <summary> An opened HCI controller; owns the device and its transport </summary>
internal sealed class HciHostAdapterDriver : IBleAdapterDriver
{
    private readonly HciDevice _device;

    private readonly HciHostObserverDriver _observer;
    private readonly HciHostBroadcasterDriver _broadcaster;
    private readonly HciHostCentralDriver _central;
    private readonly HciHostPeripheralDriver _peripheral;

    private HciHostAdapterDriver(
        HciDevice device,
        bool includeGattService,
        int maxAdvertisingDataLength,
        int numberOfAdvertisingSets
    )
    {
        _device = device;
        _observer = new HciHostObserverDriver(device);
        _peripheral = new HciHostPeripheralDriver(device, includeGattService);
        _central = new HciHostCentralDriver(device);
        _broadcaster = new HciHostBroadcasterDriver(
            device,
            maxAdvertisingDataLength,
            numberOfAdvertisingSets,
            () => RandomAddress,
            _peripheral.Find
        );
    }

    /// <summary> Resets the controller, reads what it can do and sets the random address </summary>
    public static async Task<HciHostAdapterDriver> OpenAsync(
        HciDevice device,
        bool includeGattService,
        CancellationToken cancellationToken
    )
    {
        await device.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await device.SetRandomAddressAsync(device.Address, cancellationToken).ConfigureAwait(false);
        HciLeReadMaximumAdvertisingDataLengthResult maxDataLength = await device
            .RunAsync<HciLeReadMaximumAdvertisingDataLengthCommand, HciLeReadMaximumAdvertisingDataLengthResult>(
                default,
                "read the maximum advertising data length",
                cancellationToken
            )
            .ConfigureAwait(false);
        HciLeReadNumberOfSupportedAdvertisingSetsResult sets = await device
            .RunAsync<
                HciLeReadNumberOfSupportedAdvertisingSetsCommand,
                HciLeReadNumberOfSupportedAdvertisingSetsResult
            >(default, "read the number of advertising sets", cancellationToken)
            .ConfigureAwait(false);
        return new HciHostAdapterDriver(
            device,
            includeGattService,
            maxDataLength.MaxAdvertisingDataLength,
            sets.NumSupportedAdvertisingSets
        );
    }

    public Capabilities Capabilities =>
        Capabilities.Observer
        | Capabilities.Broadcaster
        | Capabilities.Central
        | Capabilities.Peripheral
        | Capabilities.SetRandomAddress
        | Capabilities.SetGapIdentity;

    public IObserverDriver? Observer => _observer;

    public IBroadcasterDriver? Broadcaster => _broadcaster;

    public ICentralDriver? Central => _central;

    public IPeripheralDriver? Peripheral => _peripheral;

    public BleAddress RandomAddress => BleAddress.CreateRandomAddress((Data.UInt48)_device.Address);

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        _device.SetRandomAddressAsync(address.Value, cancellationToken);

    public ValueTask DisposeAsync()
    {
        _observer.Dispose();
        _broadcaster.Dispose();
        _peripheral.Dispose();
        return _device.DisposeAsync();
    }
}
