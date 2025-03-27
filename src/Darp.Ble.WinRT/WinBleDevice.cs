using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Implementation;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class WinBleDevice(IServiceProvider serviceProvider)
    : BleDevice(serviceProvider, serviceProvider.GetLogger<WinBleDevice>())
{
    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        BluetoothAdapter adapter = await BluetoothAdapter.GetDefaultAsync();
        if (!adapter.IsLowEnergySupported)
            return InitializeResult.DeviceVersionUnsupported;
        Observer = new WinBleObserver(this, ServiceProvider.GetLogger<WinBleObserver>());
        if (adapter.IsCentralRoleSupported)
            Central = new WinBleCentral(this, ServiceProvider.GetLogger<WinBleCentral>());
        if (adapter.IsAdvertisementOffloadSupported)
            Broadcaster = new WinBleBroadcaster(this, ServiceProvider.GetLogger<WinBleBroadcaster>());
        if (adapter.IsPeripheralRoleSupported)
            Peripheral = new WinBlePeripheral(this, ServiceProvider.GetLogger<WinBlePeripheral>());
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string Name => "Windows";

    /// <inheritdoc />
    public override AppearanceValues Appearance => AppearanceValues.Computer;

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.WinRT;
}
