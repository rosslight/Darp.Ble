using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Package;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Scanning on an HCI adapter (seam 1 of PR 6) </summary>
public sealed class ObserverTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // One legacy report: random address EE661991C415, RSSI -76, manufacturer data of Apple
    private static readonly HciMessage Report = HciMessage.LeEventToHost(
        "0D0110000115C4911966EE0100FF7FB40000FF0000000000000807FF4C0012020000"
    );

    [Fact]
    public async Task AScan_RunsTheControllersScan_AndDeliversItsReports()
    {
        await using var controller = new FakeController();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero));
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);

        IGapAdvertisement advertisement;
        await using (
            IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(Token).GetAsyncEnumerator(Token)
        )
        {
            ValueTask<bool> next = scan.MoveNextAsync();
            await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Extended_Scan_Enable);
            await controller.PushAsync(Report);
            (await next).ShouldBeTrue();
            advertisement = scan.Current;
        }

        advertisement.Address.ShouldBe(new BleAddress(BleAddressType.Random, (UInt48)0xEE661991C415));
        advertisement.Rssi.ShouldBe((Rssi)(-76));
        advertisement.Timestamp.ShouldBe(time.GetUtcNow());
        advertisement.Data.ToByteArray().ShouldBe(Convert.FromHexString("07FF4C0012020000"));
        await Verifier.Verify(controller.Messages);
    }

    [Fact]
    public async Task ScanTimings_AreRoundedToTheControllersUnit()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await adapter.Observer.ConfigureAsync(
            new BleScanParameters
            {
                ScanType = ScanType.Active,
                ScanInterval = TimeSpan.FromSeconds(1),
                ScanWindow = TimeSpan.FromMilliseconds(30.2),
            },
            Token
        );

        await using (
            IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(Token).GetAsyncEnumerator(Token)
        )
        {
            ValueTask<bool> next = scan.MoveNextAsync();
            await controller.PushAsync(Report);
            await next;
        }

        // Interval 1600 and window 48 units of 0.625 ms, active
        controller.Messages[0].PduBytes.ShouldBe(Convert.FromHexString("4120080100010140063000"));
    }

    [Fact]
    public async Task AScanIntervalTheControllerCannotDo_IsRejected()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await adapter.Observer.ConfigureAsync(
            new BleScanParameters { ScanInterval = TimeSpan.FromSeconds(41), ScanWindow = TimeSpan.FromSeconds(1) },
            Token
        );

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (IGapAdvertisement _ in adapter.Observer.ScanAsync(Token)) { }
        });
    }

    [Fact]
    public async Task ALostTransport_EndsTheScan()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> next = scan.MoveNextAsync();
        await controller.PushAsync(Report);
        (await next).ShouldBeTrue();

        controller.Fail(new IOException("The serial port is gone"));

        BleException exception = await Should.ThrowAsync<BleException>(() => scan.MoveNextAsync().AsTask());
        exception.InnerException.ShouldBeOfType<IOException>();
    }
}
