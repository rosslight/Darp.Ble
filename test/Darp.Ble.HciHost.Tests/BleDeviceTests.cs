using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.HciHost.Verify;
using Shouldly;
using VerifyTUnit;

namespace Darp.Ble.HciHost.Tests;

public sealed class BleDeviceTests
{
    [Test]
    public async Task InitializeBleDevice()
    {
        var address = BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.Replay(
            [
                HciMessage.CommandCompleteEventToHost("01030C00"),
                HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
                HciMessage.CommandCompleteEventToHost("01010C00"),
                HciMessage.CommandCompleteEventToHost("01012000"),
                HciMessage.CommandCompleteEventToHost("01022000FB0003"),
                HciMessage.CommandCompleteEventToHost("01052000"),
                HciMessage.CommandCompleteEventToHost("013A20003E00"),
            ]
        );
        await using IBleDevice device = await Helpers.GetBleDeviceAsync(replayTransportLayer);
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
            [HciMessage.CommandCompleteEventToHost("01052000")]
        );
        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replayTransportLayer);

        await device.SetRandomAddressAsync(newAddress);

        device.RandomAddress.ShouldBe(newAddress);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }
}
