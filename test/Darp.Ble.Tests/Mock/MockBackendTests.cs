using Darp.Ble.Data;
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
            Capabilities.Observer | Capabilities.Broadcaster | Capabilities.Central | Capabilities.SetRandomAddress
        );
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
