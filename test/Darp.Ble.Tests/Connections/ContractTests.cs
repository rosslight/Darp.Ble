using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Library service contracts and the GAP identity through the Mock (seam 4 of PR 4b) </summary>
public sealed class ContractTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource<(BleAdapter Host, AdvertisingSet Set)> _host = new();

    [Fact]
    public async Task TheGapService_IsServedWithDefaults_AndChangesReachCentrals()
    {
        await using BleAdapter adapter = await OpenAsync(_ => Task.CompletedTask);
        (BleAdapter host, AdvertisingSet set) = await _host.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: Token
        );
        RemoteGapService gap = await connection.Gatt.DiscoverGapServiceAsync(Token);

        (await gap.DeviceName.ReadAsync(Token)).ShouldBe("Mock peripheral");
        (await gap.Appearance.ReadAsync(Token)).ShouldBe(AppearanceValues.Unknown);
        host.Peripheral.DeviceName = "Sensor";
        host.Peripheral.Appearance = AppearanceValues.Computer;

        (await gap.DeviceName.ReadAsync(Token)).ShouldBe("Sensor");
        (await gap.Appearance.ReadAsync(Token)).ShouldBe(AppearanceValues.Computer);
        host.Peripheral.DeviceName.ShouldBe("Sensor");
    }

    [Fact]
    public async Task PublishingAGapService_FailsBecauseTheBackendOwnsIt()
    {
        await using BleAdapter adapter = await OpenAsync(_ => Task.CompletedTask);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            adapter.Peripheral.Gatt.AddServiceAsync(new LocalGattServiceBuilder(GapServiceContract.GapService), Token)
        );
    }

    /// <summary> Opens the mock adapter with one peripheral, set up by <paramref name="host"/>, that advertises </summary>
    private async Task<BleAdapter> OpenAsync(Func<BleAdapter, Task> host)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(async peripheral =>
                    {
                        await host(peripheral);
                        _host.SetResult(
                            (
                                peripheral,
                                await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                            )
                        );
                    })
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
