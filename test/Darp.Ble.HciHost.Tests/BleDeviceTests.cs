using Darp.Ble.Data;
using Darp.Ble.Hci.Transport;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Logging;
using VerifyTUnit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Darp.Ble.HciHost.Tests;

public sealed class BleDeviceTests
{
    [Test]
    public async Task InitializeBleDevice()
    {
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.Replay(
            HciMessage.CommandCompleteEventToHost("01030C00"),
            HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
            HciMessage.CommandCompleteEventToHost("01010C00"),
            HciMessage.CommandCompleteEventToHost("01012000"),
            HciMessage.CommandCompleteEventToHost("01022000FB0003"),
            HciMessage.CommandCompleteEventToHost("01052000"),
            HciMessage.CommandCompleteEventToHost("013A20003E00")
        );
        IBleDevice device = await GetBleDeviceAsync(replayTransportLayer);
        await device.InitializeAsync();

        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    [Test]
    public async Task SetRandomAddress()
    {
        var newAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.ReplayAfterInitialization(
            HciMessage.CommandCompleteEventToHost("01052000")
        );
        IBleDevice device = await GetAndInitializeBleDeviceAsync(replayTransportLayer);

        await device.SetRandomAddressAsync(newAddress);

        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    private static async Task<IBleDevice> GetAndInitializeBleDeviceAsync(
        ITransportLayer transportLayer,
        ulong deviceAddress = 0xE0C5AA968B6E,
        CancellationToken cancellationToken = default
    )
    {
        IBleDevice device = await GetBleDeviceAsync(transportLayer, deviceAddress, cancellationToken);
        await device.InitializeAsync(cancellationToken);
        return device;
    }

    private static async Task<IBleDevice> GetBleDeviceAsync(
        ITransportLayer transportLayer,
        ulong deviceAddress = 0xE0C5AA968B6E,
        CancellationToken cancellation = default
    )
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace)
        );
        BleManager manager = new BleManagerBuilder()
            .AddHciHost(transportLayer)
            .SetLogger(loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.SetRandomAddressAsync(
            new BleAddress(BleAddressType.RandomStatic, (UInt48)deviceAddress),
            cancellation
        );
        return device;
    }
}
