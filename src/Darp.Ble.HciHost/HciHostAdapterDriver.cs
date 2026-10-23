using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Hci;

namespace Darp.Ble.HciHost;

/// <summary> An opened HCI controller; owns the device and its transport </summary>
internal sealed class HciHostAdapterDriver : IBleAdapterDriver
{
    private readonly HciDevice _device;

    private HciHostAdapterDriver(HciDevice device)
    {
        _device = device;
    }

    /// <summary> Resets the controller, reads what it can do and sets the random address </summary>
    public static async Task<HciHostAdapterDriver> OpenAsync(HciDevice device, CancellationToken cancellationToken)
    {
        await device.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await device.SetRandomAddressAsync(device.Address, cancellationToken).ConfigureAwait(false);
        return new HciHostAdapterDriver(device);
    }

    public Capabilities Capabilities => Capabilities.SetRandomAddress;

    public IObserverDriver? Observer => null;

    public IBroadcasterDriver? Broadcaster => null;

    public ICentralDriver? Central => null;

    public IPeripheralDriver? Peripheral => null;

    public BleAddress RandomAddress => BleAddress.CreateRandomAddress((UInt48)_device.Address);

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        _device.SetRandomAddressAsync(address.Value, cancellationToken);

    public ValueTask DisposeAsync() => _device.DisposeAsync();
}
