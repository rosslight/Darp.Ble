using System.Diagnostics;
using System.Reflection;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Tests.Connections;
using Darp.Ble.Tests.Lifecycle;
using Darp.Ble.Tests.Scanning;
using Shouldly;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> Diagnostics cost nothing on the read loop and the write-without-response fast path while nobody listens (seam 4 of PR 5, D14) </summary>
/// <remarks> Measured on one thread without awaiting inside the measurement; the collection runs alone </remarks>
[Collection(TelemetryCollection.Name)]
public sealed class HotPathTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DroppingScanReports_AllocatesNothing_WhileNobodyListens()
    {
        var observer = new TestObserverDriver();
        await using BleAdapter adapter = await OpenAsync(
            new TestAdapterDriver { Capabilities = Capabilities.Observer, Observer = observer }
        );
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(new QueueOptions(1, QueueFullMode.DropNewest), Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();
        IGapAdvertisement report = TestAdvertisements.Get(1);
        observer.Report(report);
        (await first).ShouldBeTrue();
        // The queue now holds one report; every further one is dropped and counted
        observer.Report(report, report);

        long allocated = MeasureAllocations(() =>
        {
            for (var i = 0; i < 1_000; i++)
                observer.Sink.OnReport(report);
        });

        allocated.ShouldBe(0);
    }

    [Fact]
    public async Task AWriteWithoutResponseAcceptedAtOnce_AllocatesNothing_WhileNobodyListens()
    {
        var central = new TestCentralDriver();
        await using BleAdapter adapter = await OpenAsync(
            new TestAdapterDriver
            {
                Capabilities = Capabilities.Central,
                Observer = new TestObserverDriver(),
                Central = central,
            }
        );
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            BleAddress.NewRandomStaticAddress(),
            cancellationToken: Token
        );
        TestConnectionDriver driver = await central.Connection;
        Task<IReadOnlyList<RemoteGattService>> discovery = connection.Gatt.DiscoverServicesAsync(Token);
        (await driver.NextRequestAsync(Token)).Complete(BleUuid.FromUInt16(0x180F));
        RemoteGattCharacteristic characteristic = (
            await (await discovery).Single().DiscoverCharacteristicsAsync(Token)
        ).Single();
        driver.Characteristic.AcceptCommandsAtOnce = true;
        ReadOnlyMemory<byte> value = new byte[] { 1, 2, 3 };
        Write(characteristic, value);

        long allocated = MeasureAllocations(() =>
        {
            for (var i = 0; i < 1_000; i++)
                Write(characteristic, value);
        });

        allocated.ShouldBe(0);
    }

    private static void Write(RemoteGattCharacteristic characteristic, ReadOnlyMemory<byte> value)
    {
        ValueTask write = characteristic.WriteWithoutResponseAsync(value, CancellationToken.None);
        if (!write.IsCompletedSuccessfully)
            throw new InvalidOperationException("The write did not complete synchronously");
        write.GetAwaiter().GetResult();
    }

    private static long MeasureAllocations(Action action)
    {
        // A Debug build of the library runs unoptimized code, which boxes where optimized code does not
        bool optimized =
            typeof(BleManager).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled is not true;
        Assert.SkipUnless(optimized, "Allocations are measured against an optimized build of the library");
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static async Task<BleAdapter> OpenAsync(TestAdapterDriver driver)
    {
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(driver)));
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return await adapters[0].OpenAsync(Token);
    }
}
