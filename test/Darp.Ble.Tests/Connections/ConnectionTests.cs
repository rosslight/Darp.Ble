using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Connecting to mock peripherals through the public API (seam 1) </summary>
public sealed class ConnectionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.Empty.WithCompleteLocalName("Sensor");

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task ConnectAsync_ToAMockPeripheral_OpensAConnectionUntilItIsDisposed()
    {
        var advertising = new TaskCompletionSource<AdvertisingSet>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(async peripheral =>
                advertising.SetResult(await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
            )
        );
        AdvertisingSet set = await advertising.Task;

        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );

        connection.Address.ShouldBe(set.RandomAddress);
        adapter.Central.Connections.ShouldBe([connection]);
        await connection.DisposeAsync();
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
        adapter.Central.Connections.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConnectAsync_ToAnAddressNobodyAdvertises_FailsAsUnreachable()
    {
        await using BleAdapter adapter = await OpenAsync(_ => { });

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(() =>
            adapter.Central.ConnectAsync(BleAddress.NewRandomStaticAddress(), cancellationToken: Token)
        );

        exception.Kind.ShouldBe(BleConnectErrorKind.Unreachable);
        adapter.Central.Connections.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisposingTheAdapter_EndsItsConnections()
    {
        var advertising = new TaskCompletionSource<AdvertisingSet>();
        BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(async peripheral =>
                advertising.SetResult(await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
            )
        );
        AdvertisingSet set = await advertising.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );

        await adapter.DisposeAsync();

        connection.Disconnected.IsCompleted.ShouldBeTrue();
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
    }

    private async Task<BleAdapter> OpenAsync(Action<MockBackendOptions> configure)
    {
        var manager = new BleManager(builder => builder.AddMock(configure), timeProvider: _time);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
