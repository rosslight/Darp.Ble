using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Payload;
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
            HciMessage.CommandCompleteEventToHost("01030C00"),
            HciMessage.CommandCompleteEventToHost("010110000D02110D59000211"),
            HciMessage.CommandCompleteEventToHost("01010C00"),
            HciMessage.CommandCompleteEventToHost("01012000"),
            HciMessage.CommandCompleteEventToHost("01022000FB0003"),
            HciMessage.CommandCompleteEventToHost("01052000"),
            HciMessage.CommandCompleteEventToHost("013A20003E00")
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
            HciMessage.CommandCompleteEventToHost("01052000")
        );
        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replayTransportLayer);

        await device.SetRandomAddressAsync(newAddress);

        device.RandomAddress.ShouldBe(newAddress);
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }

    [Test]
    //[Timeout(5000)]
    public async Task StartObserving(CancellationToken cancellationToken)
    {
        ReplayTransportLayer replayTransportLayer = ReplayTransportLayer.ReplayAfterInitialization(
            HciMessage.CommandCompleteEventToHost("01000100A000A000"),
            HciMessage.CommandCompleteEventToHost("010000000000"),
            HciMessage.CommandCompleteEventToHost("010000000000")
        );
        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(
            replayTransportLayer,
            cancellationToken: cancellationToken
        );
        IBleObserver observer = device.Observer;

        IDisposableObservable<IGapAdvertisement> advObservable = await observer.StartObservingAsync(cancellationToken);
        Task<IGapAdvertisement> observationTask = advObservable.FirstAsync().ToTask(cancellationToken);
        replayTransportLayer.Push(
            HciMessage.LeEventToHost("0D0110000115C4911966EE0100FF7FB40000FF0000000000000807FF4C0012020000")
        );
        IGapAdvertisement advertisement = await observationTask;
        await advObservable.DisposeAsync();

        advertisement.Address.ShouldBe(new BleAddress(BleAddressType.RandomStatic, UInt48.Zero));
        await Verifier.Verify(replayTransportLayer.MessagesToController);
    }
}
