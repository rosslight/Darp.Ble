using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Tests.Lifecycle;
using Shouldly;

namespace Darp.Ble.Tests.Advertising;

/// <summary> The lifecycle of advertising sets, at the driver boundary </summary>
public sealed class BleBroadcasterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly AdvertisingParameters Slow = new()
    {
        MinPrimaryAdvertisingInterval = TimeSpan.FromSeconds(1),
        MaxPrimaryAdvertisingInterval = TimeSpan.FromSeconds(1),
    };

    private static readonly AdvertisingParameters Fast = new()
    {
        MinPrimaryAdvertisingInterval = TimeSpan.FromMilliseconds(100),
        MaxPrimaryAdvertisingInterval = TimeSpan.FromMilliseconds(100),
    };

    private readonly TestBroadcasterDriver _driver = new();

    [Fact]
    public async Task CreateAdvertisingSetAsync_CreatesAStoppedSet()
    {
        await using BleAdapter adapter = await OpenAsync();

        AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);

        _driver.Sets.ShouldHaveSingleItem().Calls.ShouldBeEmpty();
        set.Parameters.ShouldBe(Slow);
        set.Data.ShouldBe(AdvertisingData.Empty);
    }

    [Fact]
    public async Task StartAdvertisingAsync_WhenStartingFails_DisposesTheNewSet()
    {
        await using BleAdapter adapter = await OpenAsync();
        _driver.Failing.Add("start");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            adapter.Broadcaster.StartAdvertisingAsync(
                AdvertisingData.Empty,
                TimeSpan.FromSeconds(1),
                cancellationToken: Token
            )
        );

        _driver.Sets.ShouldHaveSingleItem().Calls.ShouldBe(["dispose"]);
    }

    [Fact]
    public async Task SetAdvertisingParametersAsync_WhileAdvertising_Throws()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.Empty,
            TimeSpan.FromSeconds(1),
            cancellationToken: Token
        );

        await Should.ThrowAsync<InvalidOperationException>(() => set.SetAdvertisingParametersAsync(Fast, Token));

        _driver.Sets[0].Calls.ShouldBe(["start"]);
    }

    [Fact]
    public async Task SetAdvertisingParametersAndRestartAsync_RestartsASetThatAdvertised()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.Empty,
            TimeSpan.FromSeconds(1),
            cancellationToken: Token
        );

        await set.SetAdvertisingParametersAndRestartAsync(Fast, Token);

        _driver.Sets[0].Calls.ShouldBe(["start", "stop", "parameters", "start"]);
        set.Parameters.ShouldBe(Fast);
    }

    [Fact]
    public async Task SetAdvertisingParametersAndRestartAsync_LeavesAStoppedSetStopped()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);

        await set.SetAdvertisingParametersAndRestartAsync(Fast, Token);

        _driver.Sets[0].Calls.ShouldBe(["parameters"]);
    }

    [Fact]
    public async Task SetAdvertisingParametersAndRestartAsync_WhenStartingAgainFails_LeavesTheSetStopped()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.Empty,
            TimeSpan.FromSeconds(1),
            cancellationToken: Token
        );
        _driver.Failing.Add("start");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            set.SetAdvertisingParametersAndRestartAsync(Fast, Token)
        );
        _driver.Failing.Clear();
        await set.StartAsync(Token);

        _driver.Sets[0].Calls.ShouldBe(["start", "stop", "parameters", "start"]);
    }

    [Fact]
    public async Task SetAdvertisingDataAsync_WhenTheDriverRejectsIt_KeepsTheOldData()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);
        _driver.Failing.Add("data");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            set.SetAdvertisingDataAsync(AdvertisingData.Empty.WithCompleteLocalName("New"), Token)
        );

        set.Data.ShouldBe(AdvertisingData.Empty);
    }

    [Fact]
    public async Task CreateAdvertisingSetAsync_WithAMaximumShorterThanTheMinimum_Throws()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingParameters parameters = Slow with { MaxPrimaryAdvertisingInterval = TimeSpan.FromMilliseconds(500) };

        await Should.ThrowAsync<ArgumentException>(() =>
            adapter.Broadcaster.CreateAdvertisingSetAsync(parameters, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task DisposingTheAdapter_StopsAndDisposesItsSets()
    {
        BleAdapter adapter = await OpenAsync();
        AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.Empty,
            TimeSpan.FromSeconds(1),
            cancellationToken: Token
        );
        AdvertisingSet stopped = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);

        await adapter.DisposeAsync();
        await advertising.DisposeAsync();

        _driver.Sets[0].Calls.ShouldBe(["start", "stop", "dispose"]);
        _driver.Sets[1].Calls.ShouldBe(["dispose"]);
        await Should.ThrowAsync<ObjectDisposedException>(() => stopped.StartAsync(Token));
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task SetRandomAddressAsync_WithoutTheCapability_ThrowsNotSupportedException()
    {
        await using BleAdapter adapter = await OpenAsync();
        AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);
        var address = new BleAddress(BleAddressType.Random, (UInt48)0xC1_2233_4455);

        await Should.ThrowAsync<NotSupportedException>(() => adapter.SetRandomAddressAsync(address, Token));
        await Should.ThrowAsync<NotSupportedException>(() => set.SetRandomAddressAsync(address, Token));
    }

    [Fact]
    public async Task SetRandomAddressAsync_WithTheCapability_ChangesTheAddress()
    {
        await using BleAdapter adapter = await OpenAsync(Capabilities.SetRandomAddress);
        AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(Slow, cancellationToken: Token);
        var address = new BleAddress(BleAddressType.Random, (UInt48)0xC1_2233_4455);

        await adapter.SetRandomAddressAsync(address, Token);
        await set.SetRandomAddressAsync(address, Token);

        adapter.RandomAddress.ShouldBe(address);
        set.RandomAddress.ShouldBe(address);
    }

    [Fact]
    public async Task Broadcaster_WhenTheAdapterCannotAdvertise_ThrowsNotSupportedException()
    {
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(new TestAdapterDriver())));
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        await using BleAdapter adapter = await adapters[0].OpenAsync(Token);

        Should.Throw<NotSupportedException>(() => adapter.Broadcaster);
    }

    private async Task<BleAdapter> OpenAsync(Capabilities features = Capabilities.None)
    {
        var adapterDriver = new TestAdapterDriver { Features = features, Broadcaster = _driver };
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(adapterDriver)));
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
