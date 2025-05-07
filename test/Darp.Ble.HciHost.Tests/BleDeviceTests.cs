using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Hci.Transport;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Logging;
using Shouldly;
using VerifyTUnit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Darp.Ble.HciHost.Tests;

public sealed class BleDeviceTests
{
    [Test]
    public async Task InitializeBleDevice()
    {
        var address = BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.Replay(
            HciMessage.CommandCompleteEventToHost("01030C00"),
            HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
            HciMessage.CommandCompleteEventToHost("01010C00"),
            HciMessage.CommandCompleteEventToHost("01012000"),
            HciMessage.CommandCompleteEventToHost("01022000FB0003"),
            HciMessage.CommandCompleteEventToHost("01052000"),
            HciMessage.CommandCompleteEventToHost("013A20003E00")
        );
        await using IBleDevice device = await GetBleDeviceAsync(replayTransportLayer);
        await device.InitializeAsync();

        device.IsInitialized.ShouldBeTrue();
        device.IsDisposed.ShouldBeFalse();
        device.RandomAddress.ShouldBe(address);
        device.Appearance.ShouldBe(AppearanceValues.Unknown);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    [Test]
    public async Task SetRandomAddress()
    {
        var newAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.ReplayAfterInitialization(
            HciMessage.CommandCompleteEventToHost("01052000")
        );
        await using IBleDevice device = await GetAndInitializeBleDeviceAsync(replayTransportLayer);

        await device.SetRandomAddressAsync(newAddress);

        device.RandomAddress.ShouldBe(newAddress);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    private static async Task<IBleDevice> GetAndInitializeBleDeviceAsync(
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

    private static async Task<IBleDevice> GetBleDeviceAsync(
        ITransportLayer transportLayer,
        BleAddress? deviceAddress = null,
        CancellationToken cancellation = default
    )
    {
        deviceAddress ??= BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning)
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
