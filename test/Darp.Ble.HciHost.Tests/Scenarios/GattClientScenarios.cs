using Darp.Ble.Connections;
using Darp.Ble.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> The behaviour every backend shares, run against Mock and HciHost </summary>
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
        await Should.ThrowAsync<OperationCanceledException>(cancelled.WithWatchdog());
        peer.SetValue([2]);
        Task<byte[]> next = peer.Characteristic.ReadAsync(Token);
        await peer.ReleaseAnswerAsync();
        await peer.WaitForRequestsAsync(2);
        await peer.ReleaseAnswerAsync();

        (await next.WithWatchdog()).ShouldBe([2]);
        // The next read went out only after the late answer: the cancelled read kept the slot until then
        peer.Traffic.ShouldBe(["read", "answer", "read", "answer"]);
    }

    [Fact]
    public async Task AWriteLongerThanTheMaxWritePayload_IsRejected_WithoutReachingThePeripheral()
    {
        await using IScenarioPeer peer = await ConnectAsync();
        int max = peer.Connection.MaxWritePayloadLength;

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            peer.Characteristic.WriteAsync(new byte[max + 1], Token)
        );
        await peer.Characteristic.WriteAsync(new byte[max], Token);

        peer.Requests.ShouldBe([$"write {Convert.ToHexString(new byte[max])}"]);
    }

    // A value read in several transactions comes back whole, but an attribute value has at most 512 bytes
    [Theory]
    [InlineData(300, 300)]
    [InlineData(600, 512)]
    public async Task AValueLongerThanOneAnswer_IsReadWhole_UpToTheLongestAttributeValue(int length, int expected)
    {
        await using IScenarioPeer peer = await ConnectAsync();
        byte[] value = [.. Enumerable.Range(0, length).Select(i => (byte)i)];
        peer.SetValue(value);

        byte[] read = await peer.Characteristic.ReadAsync(Token);

        read.ShouldBe(value[..expected]);
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

        await Should.ThrowAsync<BleDisconnectedException>(read.WithWatchdog());
        (await peer.Connection.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.AttTimeout);
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
        await Task.WhenAll(first, write, second).WithWatchdog();

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

        await Should.ThrowAsync<BleDisconnectedException>(read.WithWatchdog());
        (await peer.Connection.Disconnected.WithWatchdog()).Reason.ShouldBe(DisconnectReason.RemoteRequest);
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
