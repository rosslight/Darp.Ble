using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.HciHost.Verify;
using Shouldly;
using VerifyTUnit;

namespace Darp.Ble.HciHost.Tests;

public sealed class ObserverTests
{
    private static readonly HciMessage SingleAdv = HciMessage.LeEventToHost(
        "0D0110000115C4911966EE0100FF7FB40000FF0000000000000807FF4C0012020000"
    );
    private static readonly HciMessage[] ObservingMessages =
    [
        HciMessage.CommandCompleteEventToHost("01412000"),
        HciMessage.CommandCompleteEventToHost("01422000"),
        HciMessage.CommandCompleteEventToHost("01422000"),
    ];

    // start with no HCI messages so StartObservingAsync completes immediately
    private static readonly HciMessage[] EmptyMessages = [];

    private static async Task<(IBleObserver Observer, ReplayTransportLayer Replay)> CreateObserver(
        HciMessage[] messages,
        CancellationToken token
    )
    {
        ReplayTransportLayer transport = ReplayTransportLayer.ReplayAfterInitialization(messages);
        IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(transport, cancellationToken: token);

        // sanity check: not observing yet
        device.Observer.IsObserving.ShouldBeFalse();
        return (device.Observer, transport);
    }

    private static Task<(IBleObserver Observer, ReplayTransportLayer Replay)> CreateDefaultObserver(
        CancellationToken token
    ) => CreateObserver(ObservingMessages, token);

    [Test]
    [Timeout(5000)]
    public async Task AdvertisementObserved(CancellationToken token)
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(token);

        Task<IGapAdvertisement> observationTask = observer.OnAdvertisement().FirstAsync().ToTask(token);
        await observer.StartObservingAsync(token);

        replay.Push(SingleAdv);
        IGapAdvertisement advertisement = await observationTask;

        advertisement
            .AsByteArray()
            .ShouldBe(Convert.FromHexString("10000115C4911966EE0100FF7FB40000FF0000000000000807FF4C0012020000"));
        await Verifier.Verify(replay.MessagesToController);
    }

    [Test]
    [Timeout(5_000)]
    public async Task Configure(CancellationToken token)
    {
        const ScanTiming interval1 = ScanTiming.Ms100;
        const ScanTiming interval2 = ScanTiming.Ms1000;
        (IBleObserver observer, _) = await CreateDefaultObserver(token);

        bool configureSuccess1 = observer.Configure(new BleObservationParameters { ScanInterval = interval1 });
        configureSuccess1.ShouldBeTrue();
        observer.Parameters.ScanInterval.ShouldBe(interval1);

        // start observing
        await observer.StartObservingAsync(token);
        observer.IsObserving.ShouldBeTrue();

        // now configure should be rejected
        bool configureSuccess2 = observer.Configure(new BleObservationParameters { ScanInterval = interval2 });
        configureSuccess2.ShouldBeFalse();
        observer.Parameters.ScanInterval.ShouldBe(interval1);

        await observer.StopObservingAsync();
    }

    [Test]
    [Timeout(5_000)]
    public async Task StartObserving_IsNoOp(CancellationToken token)
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(token);

        await observer.StartObservingAsync(token);
        observer.IsObserving.ShouldBeTrue();

        await observer.StartObservingAsync(token);
        observer.IsObserving.ShouldBeTrue();

        await observer.StopObservingAsync();
        observer.IsObserving.ShouldBeFalse();

        await Verifier.Verify(replay.MessagesToController);
    }

    [Test]
    [Timeout(5_000)]
    public async Task StopObserving_BeforeStart_IsNoOp(CancellationToken token)
    {
        (IBleObserver observer, ReplayTransportLayer _) = await CreateObserver(EmptyMessages, token);

        await observer.StopObservingAsync();
        observer.IsObserving.ShouldBeFalse();
    }

    [Test]
    [Timeout(5_000)]
    public async Task OnAdvertisement_Unsubscribe_StopsCallback(CancellationToken ct)
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(ct);
        await observer.StartObservingAsync(ct);

        var calls = 0;
        IDisposable subscription = observer.OnAdvertisement(_ => calls++);

        replay.Push(SingleAdv);
        calls.ShouldBe(1);

        subscription.Dispose();

        replay.Push(SingleAdv);
        calls.ShouldBe(1, "no further calls after Dispose()");

        await Verifier.Verify(replay.MessagesToController);
    }

    [Test]
    [Timeout(5_000)]
    public async Task OnAdvertisement_ThrowingHandler_DoesNotPreventOthers(CancellationToken ct)
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(ct);
        await observer.StartObservingAsync(ct);

        var secondFired = false;

        observer.OnAdvertisement(_ => throw new InvalidOperationException("boom"));
        observer.OnAdvertisement(_ => secondFired = true);

        replay.Push(SingleAdv);
        secondFired.ShouldBeTrue();
    }
}
