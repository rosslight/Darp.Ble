using Windows.Devices.Bluetooth;
using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class WinBleDevice(IObserver<(BleDevice, LogEvent)>? logger) : BleDevice(logger)
{
    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        BluetoothAdapter adapter = await BluetoothAdapter.GetDefaultAsync();
        if (!adapter.IsLowEnergySupported) return InitializeResult.DeviceVersionUnsupported;
        Observer = new WinBleObserver(this, Logger);
        if (adapter.IsCentralRoleSupported)
            Central = new WinBleCentral(this, Logger);
        if (adapter.IsAdvertisementOffloadSupported)
            Broadcaster = new WinBleBroadcaster(this, Logger);
        if (adapter.IsPeripheralRoleSupported)
            Peripheral = new WinBlePeripheral(this, Logger);
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string Name => "Windows";

    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.WinRT";
}