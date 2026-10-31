using Darp.Ble.Mock;
using Shouldly;

namespace Darp.Ble.Tests.Mock;

public sealed class MockBackendTests
{
    [Fact]
    public async Task AddMock_ListsOneAdapterThatScansAdvertisesAndConnects()
    {
        var manager = new BleManager(builder => builder.AddMock(options => options.Name = "Sensor"));

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );
        BleAdapterInfo info = adapters.ShouldHaveSingleItem();
        await using BleAdapter adapter = await info.OpenAsync(TestContext.Current.CancellationToken);

        info.Backend.ShouldBe(BleBackends.Mock);
        info.Name.ShouldBe("Sensor");
        adapter.Capabilities.ShouldBe(
            Capabilities.Observer
                | Capabilities.Broadcaster
                | Capabilities.Central
                | Capabilities.Peripheral
                | Capabilities.SetRandomAddress
                | Capabilities.SetGapIdentity
        );
    }

    [Fact]
    public async Task OpenAsync_WhileTheAdapterIsOpen_Throws_AndOpensAgainOnceItIsDisposed()
    {
        var manager = new BleManager(builder => builder.AddMock());
        BleAdapterInfo info = (await manager.EnumerateAdaptersAsync(TestContext.Current.CancellationToken)).Single();
        BleAdapter first = await info.OpenAsync(TestContext.Current.CancellationToken);

        // A second open would be a separate radio world that sees nothing of the first
        BleAdapterInfo listedAgain = (
            await manager.EnumerateAdaptersAsync(TestContext.Current.CancellationToken)
        ).Single();
        await Should.ThrowAsync<InvalidOperationException>(() =>
            listedAgain.OpenAsync(TestContext.Current.CancellationToken)
        );

        await first.DisposeAsync();
        await using BleAdapter reopened = await info.OpenAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OpenAsync_AfterAnOpenFailedInAPeripheralsSetup_OpensTheAdapter()
    {
        var setups = 0;
        var manager = new BleManager(builder =>
            builder.AddMock(options =>
                options.AddPeripheral(
                    (_, _) => ++setups == 1 ? throw new InvalidOperationException("Setup failed") : Task.CompletedTask
                )
            )
        );
        BleAdapterInfo info = (await manager.EnumerateAdaptersAsync(TestContext.Current.CancellationToken)).Single();
        await Should.ThrowAsync<InvalidOperationException>(() => info.OpenAsync(TestContext.Current.CancellationToken));

        await using BleAdapter adapter = await info.OpenAsync(TestContext.Current.CancellationToken);

        setups.ShouldBe(2);
    }

    [Fact]
    public async Task OpenAdapterAsync_OpensAMockAdapterWithTheGivenName()
    {
        await using BleAdapter adapter = await MockBle.OpenAdapterAsync(
            "Physical adapter",
            TestContext.Current.CancellationToken
        );

        adapter.Backend.ShouldBe(BleBackends.Mock);
        adapter.Name.ShouldBe("Physical adapter");
    }
}
