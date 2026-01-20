using System.Reflection;
using Darp.Ble.Data;
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

        var callsB = 0;
        IDisposable subscriptionA = observer.OnAdvertisement(_ => { });

        observer.OnAdvertisement(_ =>
        {
            callsB++;
            subscriptionA.Dispose();
        });

        replay.Push(SingleAdv);

        // Regression: current implementation can invoke B twice when B disposes A during dispatch.
        callsB.ShouldBe(1);
    }

    [Fact(Timeout = 5000)]
    public async Task OnAdvertisement_DisposingMultipleOtherSubscriptions_DoesNotDoubleInvokeCurrentHandler()
    {
        (IBleObserver observer, ReplayTransportLayer replay) = await CreateDefaultObserver(Token);
        await observer.StartObservingAsync(Token);

        var callsC = 0;
        IDisposable subscriptionA = observer.OnAdvertisement(_ => { });
        IDisposable subscriptionB = observer.OnAdvertisement(_ => { });

        observer.OnAdvertisement(_ =>
        {
            callsC++;
            subscriptionA.Dispose();
            subscriptionB.Dispose();
        });

        replay.Push(SingleAdv);

        // Regression: current implementation can invoke C twice (and/or throw internally) when C disposes A/B during dispatch.
        callsC.ShouldBe(1);
    }

    [Fact(Timeout = 5000)]
    public async Task Configure_WhileStartIsPending_IsRejected()
    {
        const ScanTiming interval2 = ScanTiming.Ms1000;
        (IBleObserver observer, _) = await CreateDefaultObserver(Token);

        // Deterministic repro: block StartObservingAsync from entering the critical section by taking the semaphore.
        SemaphoreSlim semaphore = GetPrivateField<SemaphoreSlim>(observer, "_observationStartSemaphore");
        semaphore.Wait(Token);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            Task startTask = observer.StartObservingAsync(cts.Token);

            // While StartObservingAsync is pending, Configure should be rejected.
            bool configureSuccess = observer.Configure(new BleObservationParameters { ScanInterval = interval2 });

            // Regression: currently Configure() can succeed here because it is not synchronized with StartObservingAsync().
            configureSuccess.ShouldBeFalse();

            await Should.ThrowAsync<OperationCanceledException>(async () => await startTask);
        }
        finally
        {
            semaphore.Release();
        }
    }

    [Fact(Timeout = 5000)]
    public async Task IsObserving_IsTrueWhileStarting_CrossThread()
    {
        // Make StartObservingAsync take a moment so we can observe the Starting state.
        // We do this by delaying the scan-enable completion while keeping everything else identical.
        var responses = ReplayTransportLayer.InitializeBleDeviceMessages.Concat(ObservingMessages).ToArray();
        var delayedReplay = new ReplayTransportLayer(
            (message, i) =>
            {
                (HciMessage? Message, TimeSpan Delay) x = ReplayTransportLayer.IterateHciMessages(responses, i);

                // 0x2042 == LE Set Extended Scan Enable, little-endian opcode bytes 0x42 0x20.
                if (message.PduBytes.Length >= 2 && message.PduBytes[0] == 0x42 && message.PduBytes[1] == 0x20)
                    return (x.Message, TimeSpan.FromMilliseconds(200));

                return (x.Message, TimeSpan.Zero);
            },
            messagesToSkip: ReplayTransportLayer.InitializeBleDeviceMessages.Length,
            logger: null
        );

        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(delayedReplay, token: Token);
        IBleObserver observer = device.Observer;

        Task startTask = observer.StartObservingAsync(Token);

        // Wait until the observer has entered the Starting state.
        SpinWait.SpinUntil(
            () =>
                string.Equals(
                    GetPrivateField<object>(observer, "_observerState").ToString(),
                    "Starting",
                    StringComparison.Ordinal
                ),
            millisecondsTimeout: 1000
        );

        bool isObservingDuringStart = await Task.Run(() => observer.IsObserving, Token);

        await startTask;
        await observer.StopObservingAsync();

        // Regression: currently IsObserving is false in Starting state (it is only true in Observing).
        isObservingDuringStart.ShouldBeTrue();
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
