using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Tests.Lifecycle;
using Darp.Ble.Tests.Scanning;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> How the adapter's scans pause while it connects, at the observer driver boundary </summary>
public sealed class ScanPauseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly TestObserverDriver _observer = new();
    private readonly TaskCompletionSource _holdConnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TestCentralDriver _central;

    public ScanPauseTests()
    {
        _central = new TestCentralDriver { HoldConnect = _holdConnect };
    }

    [Fact]
    public async Task AConnect_StopsTheRadioScanUntilTheLinkIsUp_AndTheScanKeepsRunning()
    {
        await using BleAdapter adapter = await OpenAsync();
        await using ScanSession scan = await ScanSession.StartAsync(
            adapter.Observer.ScanAsync(Token),
            _observer,
            Adv(0)
        );

        Task<ConnectedPeripheral> connecting = ConnectAsync(adapter);
        await _central.Connecting.Task;

        _observer.IsScanning.ShouldBeFalse();
        _holdConnect.SetResult();
        await using ConnectedPeripheral connection = await connecting;
        _observer.Starts.Count.ShouldBe(2);
        _observer.Report(Adv(1));
        (await scan.TakeAsync(1)).ShouldBe([Adv(1)]);
    }

    [Fact]
    public async Task AScanStartedDuringAConnect_StartsTheRadioOnlyAfterIt()
    {
        await using BleAdapter adapter = await OpenAsync();
        Task<ConnectedPeripheral> connecting = ConnectAsync(adapter);
        await _central.Connecting.Task;

        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();

        _observer.Starts.ShouldBeEmpty();
        _holdConnect.SetResult();
        await using ConnectedPeripheral connection = await connecting;
        _observer.Starts.Count.ShouldBe(1);
        _observer.Report(Adv(1));
        (await first).ShouldBeTrue();
    }

    [Fact]
    public async Task ARadioScanThatCannotResume_EndsTheScansWithItsError_AndTheConnectionStays()
    {
        await using BleAdapter adapter = await OpenAsync();
        await using ScanSession scan = await ScanSession.StartAsync(
            adapter.Observer.ScanAsync(Token),
            _observer,
            Adv(0)
        );
        Task<ConnectedPeripheral> connecting = ConnectAsync(adapter);
        await _central.Connecting.Task;
        var failure = new BleException("The radio is gone");
        _observer.StartFailure = failure;

        _holdConnect.SetResult();

        await using ConnectedPeripheral connection = await connecting;
        adapter.Central.Connections.ShouldBe([connection]);
        (await Should.ThrowAsync<BleException>(scan.MoveNextAsync)).ShouldBeSameAs(failure);
    }

    private static Task<ConnectedPeripheral> ConnectAsync(BleAdapter adapter) =>
        adapter.Central.ConnectAsync(BleAddress.NewRandomStaticAddress(), cancellationToken: Token);

    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(builder =>
            builder.AddBackend(new TestBackend(new TestAdapterDriver { Observer = _observer, Central = _central }))
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }

    private static IGapAdvertisement Adv(byte number) => TestAdvertisements.Get(number);
}
