using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.HciHost.Verify;
using Shouldly;
using UInt48 = Darp.Ble.Data.UInt48;

namespace Darp.Ble.HciHost.Tests;

public sealed class BleDeviceTests
{
    [Fact]
    public async Task InitializeBleDevice()
    {
        var address = BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.Replay(
            ReplayTransportLayer.InitializeBleDeviceMessages
        );
        await using IBleDevice device = await Helpers.GetBleDeviceAsync(replayTransportLayer);
        await device.InitializeAsync();

        device.IsInitialized.ShouldBeTrue();
        device.IsDisposed.ShouldBeFalse();
        device.RandomAddress.ShouldBe(address);
        device.Appearance.ShouldBe(AppearanceValues.Unknown);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    [Fact]
    public async Task SetRandomAddress()
    {
        var newAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(
            [HciMessage.CommandCompleteEventToHost("01052000")]
        );
        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replayTransportLayer);

        await device.SetRandomAddressAsync(newAddress);

        device.RandomAddress.ShouldBe(newAddress);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    [Fact]
    public async Task LaunchMultipleCommandsAtTheSameTime()
    {
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.ReplayAfterInitialization(
            [HciMessage.CommandCompleteEventToHost("01392000"), HciMessage.CommandCompleteEventToHost("01352000")]
        );
        await using var device = new Hci.HciDevice(replayTransportLayer, 0x112233445566, loggerFactory: null);

        await device.InitializeAsync(CancellationToken.None);
        Task t1 = Create1();
        Task t2 = Create2();
        await Task.WhenAll(t1, t2);

        await Verifier.Verify(replayTransportLayer.MessagesToController);
        return;

        Task Create1() =>
            device.Host.QueryCommandCompletionAsync<
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
            device.Host.QueryCommandCompletionAsync<
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
