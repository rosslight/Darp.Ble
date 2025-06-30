using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using VerifyTUnit;
using UInt48 = Darp.Ble.Data.UInt48;

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

    [Test]
    public async Task LaunchMultipleCommandsAtTheSameTime()
    {
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.Replay(
            [
                // HCI_Reset
                HciMessage.CommandCompleteEventToHost("01030C00"),
                // HCI_Read_Local_Version_Information
                HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
                // HCI_Set_Event_Mask
                HciMessage.CommandCompleteEventToHost("01010C00"),
                // HCI_LE_Set_Event_Mask
                HciMessage.CommandCompleteEventToHost("01012000"),
                // HCI_LE_Read_Buffer_Size_V1
                HciMessage.CommandCompleteEventToHost("01022000FB0003"),
                HciMessage.CommandCompleteEventToHost("01392000"),
                HciMessage.CommandCompleteEventToHost("01352000"),
            ]
        );
        using var host = new Hci.HciHost(replayTransportLayer, 0x112233445566, NullLogger<Hci.HciHost>.Instance);

        await host.InitializeAsync(CancellationToken.None);
        Task t1 = Create1();
        Task t2 = Create2();
        await Task.WhenAll(t1, t2);

        await Verifier.Verify(replayTransportLayer.MessagesToController);
        return;

        Task Create1() =>
            host.QueryCommandCompletionAsync<
                HciLeSetExtendedAdvertisingEnableCommand,
                HciLeSetExtendedAdvertisingEnableResult
            >(
                new HciLeSetExtendedAdvertisingEnableCommand
                {
                    Enable = 0x01,
                    NumSets = 1,
                    AdvertisingHandle = (byte[])[0xFF],
                    Duration = (ushort[])[0],
                    MaxExtendedAdvertisingEvents = (byte[])[0],
                },
                cancellationToken: CancellationToken.None
            );

        Task Create2() =>
            host.QueryCommandCompletionAsync<
                HciLeSetAdvertisingSetRandomAddressCommand,
                HciLeSetAdvertisingSetRandomAddressResult
            >(
                new HciLeSetAdvertisingSetRandomAddressCommand
                {
                    AdvertisingHandle = 0xFF,
                    RandomAddress = 0x112233445566,
                },
                cancellationToken: CancellationToken.None
            );
    }
}
