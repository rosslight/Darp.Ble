using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class WinBleDevice(ILoggerFactory loggerFactory)
    : BleDevice(loggerFactory, loggerFactory.CreateLogger<WinBleDevice>())
{
    protected override Task SetRandomAddressAsyncCore(
        BleAddress randomAddress,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(
        CancellationToken cancellationToken
    )
    {
        BluetoothAdapter adapter = await BluetoothAdapter.GetDefaultAsync();
        if (!adapter.IsLowEnergySupported)
            return InitializeResult.DeviceVersionUnsupported;
        Observer = new WinBleObserver(this, LoggerFactory.CreateLogger<WinBleObserver>());
        if (adapter.IsCentralRoleSupported)
            Central = new WinBleCentral(this, LoggerFactory.CreateLogger<WinBleCentral>());
        if (adapter.IsAdvertisementOffloadSupported)
            Broadcaster = new WinBleBroadcaster(
                this,
                LoggerFactory.CreateLogger<WinBleBroadcaster>()
            );
        if (adapter.IsPeripheralRoleSupported)
            Peripheral = new WinBlePeripheral(this, LoggerFactory.CreateLogger<WinBlePeripheral>());
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string Name => "Windows";

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.WinRT;
}
