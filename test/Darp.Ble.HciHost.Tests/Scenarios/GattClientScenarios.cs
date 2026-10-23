using Darp.Ble.Connections;
using Shouldly;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> The behaviour every backend shares, run against Mock and HciHost (D11, D12) </summary>
public abstract class GattClientScenarios
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private protected abstract Task<IScenarioPeer> ConnectAsync();

    [Fact]
    public async Task ARequestCancelledAfterItWasSent_KeepsTheLinkUntilItsLateAnswer_AndTheNextGetsItsOwn()
    {
        await using IScenarioPeer peer = await ConnectAsync();
        peer.HoldAnswers = true;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task<byte[]> cancelled = peer.Characteristic.ReadAsync(cancel.Token);
        await peer.WaitForRequestsAsync(1);

        await cancel.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(cancelled);
        peer.SetValue([2]);
        Task<byte[]> next = peer.Characteristic.ReadAsync(Token);
        // The next read waits for the slot: the peripheral has not got it yet
        await Task.Delay(50, Token);
        peer.Requests.Count.ShouldBe(1);
        await peer.ReleaseAnswerAsync();
        await peer.WaitForRequestsAsync(2);
        await peer.ReleaseAnswerAsync();

        (await next).ShouldBe([2]);
    }

    [Fact]
    public async Task ARequestWithoutAnswer_EndsTheConnectionAfterTheAttTimeout()
    {
        await using IScenarioPeer peer = await ConnectAsync();
        peer.HoldAnswers = true;
        Task<byte[]> read = peer.Characteristic.ReadAsync(Token);
        await peer.WaitForRequestsAsync(1);

        peer.Time.Advance(TimeSpan.FromSeconds(29));
        read.IsCompleted.ShouldBeFalse();
        peer.Time.Advance(TimeSpan.FromSeconds(1));

        await Should.ThrowAsync<BleDisconnectedException>(read);
        (await peer.Connection.Disconnected).Reason.ShouldBe(DisconnectReason.AttTimeout);
    }

    [Fact]
    public async Task Requests_ReachThePeripheralInCallOrder()
    {
        await using IScenarioPeer peer = await ConnectAsync();
        peer.HoldAnswers = true;

        Task<byte[]> first = peer.Characteristic.ReadAsync(Token);
        Task write = peer.Characteristic.WriteAsync(new byte[] { 1 }, Token);
        Task<byte[]> second = peer.Characteristic.ReadAsync(Token);
        for (var i = 1; i <= 3; i++)
        {
            await peer.WaitForRequestsAsync(i);
            await peer.ReleaseAnswerAsync();
        }
        await Task.WhenAll(first, write, second);

        peer.Requests.ShouldBe(["read", "write 01", "read"]);
    }

    [Fact]
    public async Task APeripheralThatDisconnects_FailsTheWaitingRequest_AndEndsTheConnection()
    {
        await using IScenarioPeer peer = await ConnectAsync();
        peer.HoldAnswers = true;
        Task<byte[]> read = peer.Characteristic.ReadAsync(Token);
        await peer.WaitForRequestsAsync(1);

        await peer.DisconnectAsync();

        await Should.ThrowAsync<BleDisconnectedException>(read);
        (await peer.Connection.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
    }
}

/// <summary> The shared behaviour on the Mock backend </summary>
public sealed class MockGattClientScenarios : GattClientScenarios
{
    private protected override Task<IScenarioPeer> ConnectAsync() => MockPeer.ConnectAsync();
}

/// <summary> The shared behaviour on HciHost over a fake controller </summary>
public sealed class HciHostGattClientScenarios : GattClientScenarios
{
    private protected override Task<IScenarioPeer> ConnectAsync() => HciHostPeer.ConnectAsync();
}
