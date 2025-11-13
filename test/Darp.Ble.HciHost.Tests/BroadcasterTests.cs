using Darp.Ble.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class BroadcasterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<(IBleBroadcaster Observer, ReplayTransportLayer Replay)> CreateBroadcaster(
        HciMessage[] messages,
        CancellationToken token
    )
    {
        ReplayTransportLayer transport = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(messages);
        IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(transport, token: token);

        return (device.Broadcaster, transport);
    }

    [Fact(Timeout = 5000)]
    public async Task? MultipleAdvertisingSet_Disposal_ShouldBeRemoved()
    {
        (IBleBroadcaster broadcaster, ReplayTransportLayer replay) = await CreateBroadcaster(
            [
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(numSupportedAdvertisingSets: 2), // Create set 1
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(numSupportedAdvertisingSets: 2), // Create set 2
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Starting
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Stopping
                HciMessages.HciLeRemoveAdvertisingSetEvent(), // Remove sets
                HciMessages.HciLeRemoveAdvertisingSetEvent(),
            ],
            Token
        );

        IAdvertisingSet set1 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: Token);
        IAdvertisingSet set2 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: Token);

        set1.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();
        set1.ShouldBeOfType<HciAdvertisingSet>().AdvertisingHandle.ShouldBe<byte>(0);
        set2.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();
        set2.ShouldBeOfType<HciAdvertisingSet>().AdvertisingHandle.ShouldBe<byte>(1);

        await set1.StartAdvertisingAsync(cancellationToken: Token);
        set1.IsAdvertising.ShouldBeTrue();
        broadcaster.IsAdvertising.ShouldBeTrue();

        await broadcaster.Device.DisposeAsync();

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task AdvertisingSet_IsAdvertising_Disposal_ShouldBeStoppedAndRemoved()
    {
        (IBleBroadcaster broadcaster, ReplayTransportLayer replay) = await CreateBroadcaster(
            [
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(), // Create set
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Starting
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Stopping
                HciMessages.HciLeRemoveAdvertisingSetEvent(),
            ],
            Token
        );

        IAdvertisingSet set1 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: Token);
        set1.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();

        IAsyncDisposable disposable = await set1.StartAdvertisingAsync(cancellationToken: Token);
        set1.IsAdvertising.ShouldBeTrue();
        broadcaster.IsAdvertising.ShouldBeTrue();

        await disposable.DisposeAsync();
        set1.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();

        await broadcaster.Device.DisposeAsync();

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }
}
