using Darp.Ble.Connections;
using Darp.Ble.Gatt;
using Darp.Ble.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> Hosting behaviour every backend shares, run against Mock and HciHost </summary>
public abstract class HostingScenarios
{
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
        var running = new TaskCompletionSource<ConnectedCentral>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using IHostScenario scenario = await StartAsync(service =>
            service.AddCharacteristic(
                0xFF01,
                GattProperty.Read,
                onRead: async (central, cancellationToken) =>
                {
                    running.SetResult(central);
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
        ConnectedCentral central = await running.Task.WithWatchdog();

        await scenario.DisconnectAsync();

        await cancelled.Task.WithWatchdog();
        await central.Disconnected.WithWatchdog();
        (await scenario.GotAnswerAsync()).ShouldBeFalse();
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
