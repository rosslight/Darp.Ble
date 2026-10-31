using Darp.Ble.Connections;
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
        RemoteGapService gap = await connection.DiscoverGapServiceAsync(Token);

        (await gap.DeviceName.ReadAsync(Token)).ShouldBe("Mock peripheral");
        (await gap.Appearance.ReadAsync(Token)).ShouldBe(AppearanceValues.Unknown);
        host.Peripheral.DeviceName = "Sensor";
        host.Peripheral.Appearance = AppearanceValues.Computer;

        (await gap.DeviceName.ReadAsync(Token)).ShouldBe("Sensor");
        (await gap.Appearance.ReadAsync(Token)).ShouldBe(AppearanceValues.Computer);
        host.Peripheral.DeviceName.ShouldBe("Sensor");
    }

    [Fact]
    public async Task AddingAGapService_FailsBecauseTheBackendOwnsIt()
    {
        await using BleAdapter adapter = await OpenAsync(_ => Task.CompletedTask);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(GapServiceContract.GapService), Token)
        );
    }

    [Fact]
    public async Task TheBatteryService_AnswersReadsWithTheLatestLevel_AndNotifiesChanges()
    {
        var hosted = new TaskCompletionSource<LocalBatteryService>();
        await using BleAdapter adapter = await OpenAsync(async host =>
            hosted.SetResult(await host.Peripheral.AddBatteryServiceAsync(initialBatteryLevel: 80))
        );
        ConnectedPeripheral connection = await ConnectAsync(adapter);
        RemoteBatteryService battery = await connection.DiscoverBatteryServiceAsync(Token);
        TypedRemoteCharacteristic<byte, Properties.Notify> notifying = battery.NotifyingBatteryLevel.ShouldNotBeNull();
        using NotificationStream<byte> levels = notifying.Subscribe();
        await notifying.EnableNotificationsAsync(Token);

        (await battery.BatteryLevel.ReadAsync(Token)).ShouldBe((byte)80);
        await (await hosted.Task).SetBatteryLevelAsync(42, Token);

        (await levels.FirstAsync(Token)).ShouldBe((byte)42);
        (await battery.BatteryLevel.ReadAsync(Token)).ShouldBe((byte)42);
        RemoteDescriptor description = (
            await battery.BatteryLevel.Characteristic.DiscoverDescriptorsAsync(Token)
        ).Single();
        (await description.ReadAsync(Token)).ShouldBe("main"u8.ToArray());
    }

    [Fact]
    public async Task ABatteryLevelWithoutNotify_IsDiscoveredForReading()
    {
        await using BleAdapter adapter = await OpenAsync(host =>
        {
            var builder = new LocalGattServiceBuilder(BatteryServiceContract.BatteryService);
            builder.AddCharacteristic(
                CharacteristicDeclaration.Create<byte, Properties.Read>(0x2A19),
                onRead: (_, _) => ValueTask.FromResult((byte)30)
            );
            return host.Peripheral.AddServiceAsync(builder, Token);
        });
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        RemoteBatteryService battery = await connection.DiscoverBatteryServiceAsync(Token);

        (await battery.BatteryLevel.ReadAsync(Token)).ShouldBe((byte)30);
        battery.NotifyingBatteryLevel.ShouldBeNull();
    }

    [Fact]
    public async Task TheHeartRateService_RoundTripsMeasurements_AndResetsTheEnergyThroughItsControlPoint()
    {
        var hosted = new TaskCompletionSource<LocalHeartRateService>();
        var resets = 0;
        await using BleAdapter adapter = await OpenAsync(async host =>
            hosted.SetResult(
                await host.Peripheral.AddHeartRateServiceAsync(
                    HeartRateBodySensorLocation.Wrist,
                    onResetExpendedEnergy: () => resets++
                )
            )
        );
        ConnectedPeripheral connection = await ConnectAsync(adapter);
        RemoteHeartRateService heartRate = await connection.DiscoverHeartRateServiceAsync(Token);
        using NotificationStream<HeartRateMeasurement> measurements = heartRate.HeartRateMeasurement.Subscribe();
        await heartRate.HeartRateMeasurement.EnableNotificationsAsync(Token);
        var measurement = new HeartRateMeasurement(300)
        {
            EnergyExpended = 12,
            IsSensorContactDetected = true,
            RrIntervals = new ushort[] { 1024, 980 },
        };

        await (await hosted.Task).HeartRateMeasurement.NotifyAllAsync(measurement, Token);
        await (heartRate.HeartRateControlPoint ?? throw new InvalidOperationException("No control point")).WriteAsync(
            new byte[] { 0x01 },
            Token
        );

        HeartRateMeasurement received = await measurements.FirstAsync(Token);
        received.Value.ShouldBe((ushort)300);
        received.EnergyExpended.ShouldBe((ushort)12);
        received.IsSensorContactDetected.ShouldBe(true);
        received.RrIntervals.ToArray().ShouldBe([1024, 980]);
        resets.ShouldBe(1);
        (
            await (heartRate.BodySensorLocation ?? throw new InvalidOperationException("No location")).ReadAsync(Token)
        ).ShouldBe(HeartRateBodySensorLocation.Wrist);
        (
            await Should.ThrowAsync<GattException>(() =>
                heartRate.HeartRateControlPoint.WriteAsync(new byte[] { 0x02 }, Token)
            )
        ).AttStatus.ShouldBe((GattProtocolStatus)0x80);
    }

    [Fact]
    public async Task TheDeviceInformationService_HasOnlyTheFieldsTheHostSet()
    {
        byte[] pnpId = [0x01, 0x34, 0x12, 0x78, 0x56, 0x00, 0x01];
        await using BleAdapter adapter = await OpenAsync(host =>
            host.Peripheral.AddDeviceInformationServiceAsync(
                manufacturerName: "Darp",
                serialNumber: "1234",
                systemId: new SystemId(0x12_3456_789A, 0xABCDEF),
                pnpId: pnpId
            )
        );
        // The service keeps its own copy
        pnpId[0] = 0xFF;
        ConnectedPeripheral connection = await ConnectAsync(adapter);

        RemoteDeviceInformationService information = await connection.DiscoverDeviceInformationServiceAsync(Token);

        (await (information.ManufacturerName ?? throw new InvalidOperationException()).ReadAsync(Token)).ShouldBe(
            "Darp"
        );
        (await (information.SerialNumber ?? throw new InvalidOperationException()).ReadAsync(Token)).ShouldBe("1234");
        (await (information.SystemId ?? throw new InvalidOperationException()).ReadAsync(Token)).ShouldBe(
            new SystemId(0x12_3456_789A, 0xABCDEF)
        );
        (await (information.PnPId ?? throw new InvalidOperationException()).ReadAsync(Token)).ShouldBe([
            0x01,
            0x34,
            0x12,
            0x78,
            0x56,
            0x00,
            0x01,
        ]);
        information.ModelNumber.ShouldBeNull();
        information.RegulatoryCertificationData.ShouldBeNull();
    }

    [Fact]
    public async Task AnEchoQuery_GetsTheReplyToItsRequest_AndTimesOutWithoutOne()
    {
        var answer = true;
        await using BleAdapter adapter = await OpenAsync(host =>
            host.Peripheral.AddEchoServiceAsync(
                0xFD4C,
                0xFF71,
                0xFF72,
                async (_, request, ct) =>
                {
                    if (!answer)
                        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return [.. request.ToArray().Reverse()];
                }
            )
        );
        ConnectedPeripheral connection = await ConnectAsync(adapter);
        RemoteEchoService echo = await connection.DiscoverEchoServiceAsync(0xFD4C, 0xFF71, 0xFF72, Token);

        (await echo.QueryOneAsync(new byte[] { 1, 2, 3 }, cancellationToken: Token)).ShouldBe([3, 2, 1]);
        answer = false;
        Task<byte[]> unanswered = echo.QueryOneAsync(new byte[] { 4 }, TimeSpan.FromSeconds(5), Token);
        // The deadline runs on the manager's clock from the call on
        _time.Advance(TimeSpan.FromSeconds(5));

        await Should.ThrowAsync<TimeoutException>(() => unanswered);
    }

    [Fact]
    public async Task AfterAnEchoQueryGotNoReply_FurtherQueriesOnThatConnectionFail_AlsoThroughAnotherWrapper()
    {
        var requested = new TaskCompletionSource();
        await using BleAdapter adapter = await OpenAsync(host =>
            host.Peripheral.AddEchoServiceAsync(
                0xFD4C,
                0xFF71,
                0xFF72,
                async (_, request, ct) =>
                {
                    requested.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return request.ToArray();
                }
            )
        );
        ConnectedPeripheral connection = await ConnectAsync(adapter);
        RemoteEchoService echo = await connection.DiscoverEchoServiceAsync(0xFD4C, 0xFF71, 0xFF72, Token);
        // Discovered before the unanswered write takes the connection's ATT slot
        RemoteEchoService again = await connection.DiscoverEchoServiceAsync(0xFD4C, 0xFF71, 0xFF72, Token);
        Task<byte[]> unanswered = echo.QueryOneAsync(new byte[] { 1 }, TimeSpan.FromSeconds(5), Token);
        // The request reached the peripheral before the deadline passes
        await requested.Task;
        _time.Advance(TimeSpan.FromSeconds(5));
        await Should.ThrowAsync<TimeoutException>(() => unanswered);

        // A late reply to the first request could otherwise be taken for the next one's
        await Should.ThrowAsync<InvalidOperationException>(() =>
            again.QueryOneAsync(new byte[] { 2 }, cancellationToken: Token)
        );
    }

    private async Task<ConnectedPeripheral> ConnectAsync(BleAdapter adapter)
    {
        (_, AdvertisingSet set) = await _host.Task;
        return await adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: Token);
    }

    /// <summary> Opens the mock adapter with one peripheral, set up by <paramref name="host"/>, that advertises </summary>
    private async Task<BleAdapter> OpenAsync(Func<BleAdapter, Task> host)
    {
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            await host(peripheral);
                            _host.SetResult(
                                (
                                    peripheral,
                                    await peripheral.Broadcaster.StartAdvertisingAsync(AdvertisingData.Empty, Interval)
                                )
                            );
                        }
                    )
                ),
            timeProvider: _time
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
