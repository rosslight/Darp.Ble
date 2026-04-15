using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed partial class ObserverTests
{
    [Fact(Timeout = 5000)]
    public async Task OnAdvertisement_DisposingOtherSubscription_DoesNotDoubleInvokeCurrentHandler()
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(Token);
        await observer.StartObservingAsync(Token);

        var numberOfCalls = 0;
        IDisposable subscriptionA = observer.OnAdvertisement(_ => { });

        observer.OnAdvertisement(_ =>
        {
            numberOfCalls++;
            subscriptionA.Dispose();
        });

        replay.Push(SingleAdv);

        // Even if we dispose the subscription in the handler we should still receive a single advertisement only
        numberOfCalls.ShouldBe(1);
    }

    [Fact(Timeout = 5000)]
    public async Task OnAdvertisement_DisposingMultipleOtherSubscriptions_DoesNotDoubleInvokeCurrentHandler()
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(Token);
        await observer.StartObservingAsync(Token);

        var numberOfCalls = 0;
        IDisposable subscriptionA = observer.OnAdvertisement(_ => { });
        IDisposable subscriptionB = observer.OnAdvertisement(_ => { });

        observer.OnAdvertisement(_ =>
        {
            numberOfCalls++;
            subscriptionA.Dispose();
            subscriptionB.Dispose();
        });

        replay.Push(SingleAdv);

        // Even if we dispose multiple subscriptions in the handler we should still receive a single advertisement only
        numberOfCalls.ShouldBe(1);
    }

    [Fact(Timeout = 5000)]
    public async Task Configure_WhileStartIsPending_IsRejected()
    {
        const ScanTiming interval2 = ScanTiming.Ms1000;
        (IBleObserver observer, _) = await CreateDefaultObserver(Token);

        // Deterministic repro: block StartObservingAsync from entering the critical section by taking the semaphore.
        var semaphore = GetPrivateField<SemaphoreSlim>(observer, "_startStopSemaphore");
        await semaphore.WaitAsync(Token);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            Task startTask = observer.StartObservingAsync(cts.Token);

            // While StartObservingAsync is pending, Configure should be rejected.
            bool configureSuccess = observer.Configure(new BleObservationParameters { ScanInterval = interval2 });

            // Configure() should not be able to succeed here because because we are starting.
            configureSuccess.ShouldBeFalse();

            await Should.ThrowAsync<OperationCanceledException>(async () => await startTask);
        }
        finally
        {
            semaphore.Release();
        }
    }

    [Fact(Timeout = 5000)]
    public async Task TransportFailureWhileObserving_FaultsAdvertisementObservable()
    {
        var transport = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(ObservingMessages);
        IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(transport, token: Token);
        IBleObserver observer = device.Observer;
        Task<IGapAdvertisement> observationTask = observer.OnAdvertisement().FirstAsync().ToTask(Token);

        await observer.StartObservingAsync(Token);
        observer.IsObserving.ShouldBeTrue();

        transport.Fail(new InvalidOperationException("Serial connection lost"));

        var exception = await Should.ThrowAsync<BleObservationException>(() => observationTask);
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        observer.IsObserving.ShouldBeFalse();

        await observer.StopObservingAsync();
        await device.DisposeAsync();
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        Type? type = obj.GetType();
        while (type is not null)
        {
            FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null)
                return (T)field.GetValue(obj)!;
            type = type.BaseType;
        }

        throw new MissingFieldException(obj.GetType().FullName, fieldName);
    }
}
