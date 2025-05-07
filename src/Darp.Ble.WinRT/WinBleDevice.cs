using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Implementation;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class WinBleDevice(IServiceProvider serviceProvider)
    : BleDevice(serviceProvider, serviceProvider.GetLogger<WinBleDevice>())
{
    private BluetoothAdapter? _adapter;
    public override BleAddress RandomAddress =>
        _adapter?.BluetoothAddress is null
            ? BleAddress.NotAvailable
            : new BleAddress(BleAddressType.RandomStatic, (UInt48)_adapter.BluetoothAddress);

    protected override Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        _adapter = await BluetoothAdapter.GetDefaultAsync();
        if (!_adapter.IsLowEnergySupported)
            return InitializeResult.DeviceVersionUnsupported;
        Observer = new WinBleObserver(this, ServiceProvider.GetLogger<WinBleObserver>());
        if (_adapter.IsCentralRoleSupported)
            Central = new WinBleCentral(this, ServiceProvider.GetLogger<WinBleCentral>());
        if (_adapter.IsAdvertisementOffloadSupported)
            Broadcaster = new WinBleBroadcaster(this, ServiceProvider.GetLogger<WinBleBroadcaster>());
        if (_adapter.IsPeripheralRoleSupported)
            Peripheral = new WinBlePeripheral(this, ServiceProvider.GetLogger<WinBlePeripheral>());
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string? Name { get; set; } = "Windows";

    /// <inheritdoc />
    public override AppearanceValues Appearance { get; set; } = AppearanceValues.Computer;

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.WinRT;
}
