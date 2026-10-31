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

    private HciHostAdapterDriver(HciDevice device, int maxAdvertisingDataLength, int numberOfAdvertisingSets)
    {
        _device = device;
        _observer = new HciHostObserverDriver(device);
        _central = new HciHostCentralDriver(device);
        _broadcaster = new HciHostBroadcasterDriver(
            device,
            maxAdvertisingDataLength,
            numberOfAdvertisingSets,
            () => RandomAddress
        );
    }

    /// <summary> Resets the controller, reads what it can do and sets the random address </summary>
    public static async Task<HciHostAdapterDriver> OpenAsync(HciDevice device, CancellationToken cancellationToken)
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
            maxDataLength.MaxAdvertisingDataLength,
            sets.NumSupportedAdvertisingSets
        );
    }

    public Capabilities Features => Capabilities.SetRandomAddress;

    public IObserverDriver? Observer => _observer;

    public IBroadcasterDriver? Broadcaster => _broadcaster;

    public ICentralDriver? Central => _central;

    public IPeripheralDriver? Peripheral => null;

    public BleAddress RandomAddress => BleAddress.CreateRandomAddress((Data.UInt48)_device.Address);

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        _device.SetRandomAddressAsync(address.Value, cancellationToken);

    public ValueTask DisposeAsync()
    {
        _observer.Dispose();
        return _device.DisposeAsync();
    }
}
