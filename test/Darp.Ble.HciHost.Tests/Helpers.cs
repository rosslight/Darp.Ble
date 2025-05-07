using Darp.Ble.Data;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Tests;

public static class Helpers
{
    public static async Task<IBleDevice> GetAndInitializeBleDeviceAsync(
        ITransportLayer transportLayer,
        BleAddress? deviceAddress = null,
        CancellationToken cancellationToken = default
    )
    {
        deviceAddress ??= BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        IBleDevice device = await GetBleDeviceAsync(transportLayer, deviceAddress, cancellationToken);
        await device.InitializeAsync(cancellationToken);
        return device;
    }

    public static async Task<IBleDevice> GetBleDeviceAsync(
        ITransportLayer transportLayer,
        BleAddress? deviceAddress = null,
        CancellationToken cancellation = default
    )
    {
        deviceAddress ??= BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace)
        );
        BleManager manager = new BleManagerBuilder()
            .AddHciHost(transportLayer)
            .SetLogger(loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.SetRandomAddressAsync(deviceAddress, cancellation);
        return device;
    }
}
