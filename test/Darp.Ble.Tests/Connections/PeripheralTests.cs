using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Hosting and accepting connections on a mock peripheral through the public API (seam 4) </summary>
public sealed class PeripheralTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private static readonly AdvertisingData SensorData = AdvertisingData.Empty.WithCompleteLocalName("Sensor");

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task ACentralThatConnects_ShowsUpUntilItDisconnects()
    {
        var advertising = new TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(
                        (peripheral, await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                    )
            )
        );
        (BleAdapter peripheral, AdvertisingSet set) = await advertising.Task;

        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );

        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();
        central.Address.ShouldBe(adapter.RandomAddress);
        await connection.DisposeAsync();
        (await central.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
        peripheral.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisconnectingACentral_EndsTheConnectionOnBothSides()
    {
        var advertising = new TaskCompletionSource<(BleAdapter Peripheral, AdvertisingSet Set)>();
        await using BleAdapter adapter = await OpenAsync(options =>
            options.AddPeripheral(
                async (peripheral, _) =>
                    advertising.SetResult(
                        (peripheral, await peripheral.Broadcaster.StartAdvertisingAsync(SensorData, Interval))
                    )
            )
        );
        (BleAdapter peripheral, AdvertisingSet set) = await advertising.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        ConnectedCentral central = peripheral.Peripheral.ConnectedCentrals.ShouldHaveSingleItem();

        await central.DisconnectAsync(Token);

        (await central.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
        adapter.Central.Connections.ShouldBeEmpty();
        peripheral.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    private async Task<BleAdapter> OpenAsync(Action<MockBackendOptions> configure)
    {
        var manager = new BleManager(builder => builder.AddMock(configure), timeProvider: _time);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
