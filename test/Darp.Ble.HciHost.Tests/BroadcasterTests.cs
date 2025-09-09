using Darp.Ble.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class BroadcasterTests
{
    private static async Task<(IBleBroadcaster Observer, ReplayTransportLayer Replay)> CreateBroadcaster(
        HciMessage[] messages,
        CancellationToken token
    )
    {
        ReplayTransportLayer transport = ReplayTransportLayer.ReplayAfterInitialization(messages);
        IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(transport, cancellationToken: token);

        return (device.Broadcaster, transport);
    }

    [Fact(Timeout = 5000)]
    public async Task? MultipleAdvertisingSet_Disposal_ShouldBeRemoved()
    {
        var token = CancellationToken.None;
        (IBleBroadcaster broadcaster, ReplayTransportLayer replay) = await CreateBroadcaster(
            [
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(numSupportedAdvertisingSets: 2), // Create set 1
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(numSupportedAdvertisingSets: 2), // Create set 2
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeRemoveAdvertisingSetEvent(), // Remove sets
                HciMessages.HciLeRemoveAdvertisingSetEvent(),
            ],
            token
        );

        IAdvertisingSet set1 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: token);
        IAdvertisingSet set2 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: token);

        set1.IsAdvertising.ShouldBeFalse();
        set1.ShouldBeOfType<HciAdvertisingSet>().AdvertisingHandle.ShouldBe<byte>(0);
        set2.IsAdvertising.ShouldBeFalse();
        set2.ShouldBeOfType<HciAdvertisingSet>().AdvertisingHandle.ShouldBe<byte>(1);

        await broadcaster.Device.DisposeAsync();

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task AdvertisingSet_IsAdvertising_Disposal_ShouldBeStoppedAndRemoved()
    {
        var token = CancellationToken.None;
        (IBleBroadcaster broadcaster, ReplayTransportLayer replay) = await CreateBroadcaster(
            [
                HciMessages.HciLeReadNumberOfSupportedAdvertisingSetsEvent(), // Create set
                HciMessages.HciLeSetExtendedAdvertisingParametersEvent(),
                HciMessages.HciLeSetAdvertisingSetRandomAddressEvent(),
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Starting
                HciMessages.HciLeSetExtendedAdvertisingEnableEvent(), // Stopping
                HciMessages.HciLeRemoveAdvertisingSetEvent(),
            ],
            token
        );

        IAdvertisingSet set1 = await broadcaster.CreateAdvertisingSetAsync(cancellationToken: token);
        set1.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();

        IAsyncDisposable disposable = await set1.StartAdvertisingAsync(cancellationToken: token);
        set1.IsAdvertising.ShouldBeTrue();
        broadcaster.IsAdvertising.ShouldBeTrue();

        await disposable.DisposeAsync();
        set1.IsAdvertising.ShouldBeFalse();
        broadcaster.IsAdvertising.ShouldBeFalse();

        await broadcaster.Device.DisposeAsync();

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }
}
