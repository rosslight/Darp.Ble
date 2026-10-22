using Darp.Ble.Data;
using Shouldly;

namespace Darp.Ble.Mock.Tests;

public sealed class MockBackendTests
{
    [Fact]
    public async Task AddMock_ListsOneMockAdapterWithTheConfiguredName()
    {
        var manager = new BleManager(builder => builder.AddMock(options => options.Name = "Sensor"));

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );

        BleAdapterInfo adapter = adapters.ShouldHaveSingleItem();
        adapter.Backend.ShouldBe(BleBackends.Mock);
        adapter.Name.ShouldBe("Sensor");
    }

    [Fact]
    public async Task OpenAdapterAsync_OpensAMockAdapterThatSupportsEveryRole()
    {
        await using BleAdapter adapter = await MockBle.OpenAdapterAsync(
            "Physical adapter",
            TestContext.Current.CancellationToken
        );

        adapter.Backend.ShouldBe(BleBackends.Mock);
        adapter.Name.ShouldBe("Physical adapter");
        adapter.Capabilities.ShouldBe(Capabilities.Central | Capabilities.Peripheral);
    }

    [Fact]
    public async Task OpenAdapterAsync_WhenCancelled_ThrowsTheCallersCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            MockBle.OpenAdapterAsync(cancellationToken: cancellation.Token)
        );

        exception.CancellationToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    public async Task OpenAsync_WhenCancelled_ThrowsTheCallersCancellation()
    {
        var manager = new BleManager(builder => builder.AddMock());
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            adapters[0].OpenAsync(cancellation.Token)
        );

        exception.CancellationToken.ShouldBe(cancellation.Token);
    }
}
