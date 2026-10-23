using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Shouldly;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> Hosting behaviour every backend shares, run against Mock and HciHost (D9, D12) </summary>
public abstract class HostingScenarios
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private protected abstract Task<IHostScenario> StartAsync(Action<LocalGattServiceBuilder> build);

    [Fact]
    public async Task AWriteHandlersStatus_IsTheCentralsAnswer()
    {
        await using IHostScenario scenario = await StartAsync(service =>
            service.AddCharacteristic(
                0xFF01,
                GattProperty.Read | GattProperty.Write,
                onWrite: (_, value, _) =>
                    ValueTask.FromResult(
                        value.Span[0] == 0xFF ? GattProtocolStatus.WriteRequestRejected : GattProtocolStatus.Success
                    )
            )
        );

        (await scenario.WriteAsync([0x01])).ShouldBe(GattProtocolStatus.Success);
        (await scenario.WriteAsync([0xFF])).ShouldBe(GattProtocolStatus.WriteRequestRejected);
    }

    [Fact]
    public async Task AHandlerRunningWhenItsCentralDisconnects_IsCancelled_AndNotAnswered()
    {
        var running = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using IHostScenario scenario = await StartAsync(service =>
            service.AddCharacteristic(
                0xFF01,
                GattProperty.Read,
                onRead: async (_, cancellationToken) =>
                {
                    running.SetResult();
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled.SetResult();
                        throw;
                    }
                    return [];
                }
            )
        );
        await scenario.StartReadAsync();
        await running.Task.WaitAsync(Token);

        await scenario.DisconnectAsync();

        await cancelled.Task.WaitAsync(Token);
        (await scenario.GotAnswerAsync()).ShouldBeFalse();
        while (scenario.Host.Peripheral.ConnectedCentrals.Count > 0)
            await Task.Delay(5, Token);
    }
}

/// <summary> The shared hosting behaviour on the Mock backend </summary>
public sealed class MockHostingScenarios : HostingScenarios
{
    private protected override Task<IHostScenario> StartAsync(Action<LocalGattServiceBuilder> build) =>
        MockHostScenario.StartAsync(build);
}

/// <summary> The shared hosting behaviour on HciHost over a fake controller </summary>
public sealed class HciHostHostingScenarios : HostingScenarios
{
    private protected override Task<IHostScenario> StartAsync(Action<LocalGattServiceBuilder> build) =>
        HciHostHostScenario.StartAsync(build);
}
