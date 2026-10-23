using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Tests.Lifecycle;
using Darp.Ble.Tests.Scanning;
using Shouldly;

namespace Darp.Ble.Tests.Connections;

/// <summary> Descriptors of a discovered characteristic, at the driver boundary (seam 1; hosted descriptors come with typed hosting) </summary>
public sealed class DescriptorTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly TestCentralDriver _central = new();

    [Fact]
    public async Task ADiscoveredDescriptor_IsWrittenAndRead_ThroughItsConnection()
    {
        await using BleAdapter adapter = await OpenAsync();
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            BleAddress.NewRandomStaticAddress(),
            cancellationToken: Token
        );
        TestConnectionDriver driver = await _central.Connection;
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.DiscoverServicesAsync(Token);
        (await driver.NextRequestAsync(Token)).Complete(BleUuid.FromUInt16(0x180F));
        RemoteGattCharacteristic characteristic = (
            await (await discovery).Single().DiscoverCharacteristicsAsync(Token)
        ).Single();

        RemoteDescriptor description = (await characteristic.DiscoverDescriptorsAsync(Token)).Single();
        await description.WriteAsync(new byte[] { 1, 2 }, Token);

        description.Uuid.ShouldBe(BleUuid.FromUInt16(0x2901));
        (await description.ReadAsync(Token)).ShouldBe([1, 2]);
    }

    private async Task<BleAdapter> OpenAsync()
    {
        var manager = new BleManager(builder =>
            builder.AddBackend(
                new TestBackend(new TestAdapterDriver { Observer = new TestObserverDriver(), Central = _central })
            )
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
