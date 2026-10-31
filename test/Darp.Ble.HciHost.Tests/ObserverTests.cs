using System.Buffers.Binary;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Package;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Scanning on an HCI adapter </summary>
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

    // Event type bits of extended reports: the scan response flag, and the data status in bits 5 and 6
    private const ushort ScanResponse = 0b01000;
    private const ushort MoreDataToCome = 0b01 << 5;
    private const ushort Truncated = 0b10 << 5;

    // Ten manufacturer data sections of 31 bytes each, so 310 bytes; a controller has to split them
    private static readonly byte[] LongData =
    [
        .. Enumerable.Range(0, 10).SelectMany(i => (byte[])[0x1E, 0xFF, .. Enumerable.Repeat((byte)i, 29)]),
    ];

    /// <summary> One extended report from a random address, with advertising SID 1 </summary>
    private static HciMessage ExtendedReport(
        ushort eventType,
        ulong address,
        ReadOnlySpan<byte> data,
        sbyte txPower = 0x7F
    )
    {
        byte[] parameters = new byte[2 + 24 + data.Length];
        parameters[0] = 0x0D;
        parameters[1] = 1;
        Span<byte> report = parameters.AsSpan(2);
        BinaryPrimitives.WriteUInt16LittleEndian(report, eventType);
        report[2] = 0x01;
        BinaryPrimitives.WriteUInt64LittleEndian(report[3..11], address);
        report[9] = 0x01;
        report[10] = 0x02;
        report[11] = 0x01;
        report[12] = (byte)txPower;
        report[13] = 0xB4;
        report[16] = 0xFF;
        report[23] = (byte)data.Length;
        data.CopyTo(report[24..]);
        return HciMessage.LeEventToHost(parameters);
    }

    private static async Task<List<IGapAdvertisement>> ScanAsync(int count, params HciMessage[] events)
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var heard = new List<IGapAdvertisement>();
        await using IAsyncEnumerator<IGapAdvertisement> scan = adapter
            .Observer.ScanAsync(Token)
            .GetAsyncEnumerator(Token);
        ValueTask<bool> first = scan.MoveNextAsync();
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Extended_Scan_Enable);
        foreach (HciMessage message in events)
            await controller.PushAsync(message);
        (await first).ShouldBeTrue();
        heard.Add(scan.Current);
        while (heard.Count < count)
        {
            (await scan.MoveNextAsync()).ShouldBeTrue();
            heard.Add(scan.Current);
        }
        return heard;
    }

    [Fact]
    public async Task AnAdvertisementInSeveralReports_ArrivesAsOne()
    {
        List<IGapAdvertisement> heard = await ScanAsync(
            1,
            ExtendedReport(MoreDataToCome, 0xC0FFEE, LongData.AsSpan(0, 200)),
            ExtendedReport(0, 0xC0FFEE, LongData.AsSpan(200))
        );

        IGapAdvertisement advertisement = heard.ShouldHaveSingleItem();
        advertisement.Data.ToByteArray().ShouldBe(LongData);
        advertisement.Data.Count.ShouldBe(10);
        advertisement.EventType.ShouldBe(BleEventType.None);
    }

    [Fact]
    public async Task ReportsOfAnotherAdvertiser_InBetween_StayApart()
    {
        List<IGapAdvertisement> heard = await ScanAsync(
            2,
            ExtendedReport(MoreDataToCome, 0xC0FFEE, LongData.AsSpan(0, 200)),
            ExtendedReport(0, 0xBEEF, [0x02, 0x01, 0x06]),
            ExtendedReport(0, 0xC0FFEE, LongData.AsSpan(200))
        );

        heard[0].Address.Value.ShouldBe((UInt48)0xBEEF);
        heard[0].Data.ToByteArray().ShouldBe([0x02, 0x01, 0x06]);
        heard[1].Address.Value.ShouldBe((UInt48)0xC0FFEE);
        heard[1].Data.ToByteArray().ShouldBe(LongData);
    }

    [Fact]
    public async Task AScanResponse_IsNotJoinedWithTheAdvertisement()
    {
        List<IGapAdvertisement> heard = await ScanAsync(
            2,
            ExtendedReport(MoreDataToCome, 0xC0FFEE, LongData.AsSpan(0, 200)),
            ExtendedReport(ScanResponse, 0xC0FFEE, [0x02, 0x01, 0x06]),
            ExtendedReport(0, 0xC0FFEE, LongData.AsSpan(200))
        );

        heard[0].EventType.ShouldBe(BleEventType.ScanResponse);
        heard[0].Data.ToByteArray().ShouldBe([0x02, 0x01, 0x06]);
        heard[1].Data.ToByteArray().ShouldBe(LongData);
    }

    [Fact]
    public async Task TruncatedData_ArrivesAsTheControllerReceivedIt()
    {
        List<IGapAdvertisement> heard = await ScanAsync(
            1,
            ExtendedReport(MoreDataToCome, 0xC0FFEE, LongData.AsSpan(0, 124)),
            ExtendedReport(Truncated, 0xC0FFEE, LongData.AsSpan(124, 62))
        );

        heard.ShouldHaveSingleItem().Data.ToByteArray().ShouldBe(LongData[..186]);
    }

    [Fact]
    public async Task TheTxPowerOfAnEarlierPart_StaysWhenLaterPartsLeaveItOut()
    {
        List<IGapAdvertisement> heard = await ScanAsync(
            1,
            ExtendedReport(MoreDataToCome, 0xC0FFEE, LongData.AsSpan(0, 200), txPower: 4),
            ExtendedReport(0, 0xC0FFEE, LongData.AsSpan(200))
        );

        heard.ShouldHaveSingleItem().TxPower.ShouldBe((TxPowerLevel)4);
    }

    [Fact]
    public async Task AnAdvertisementLongerThanAnyCanBe_IsDroppedWithAllItsParts()
    {
        HciMessage[] tooLong =
        [
            .. Enumerable.Range(0, 9).Select(_ => ExtendedReport(MoreDataToCome, 0xC0FFEE, new byte[200])),
            ExtendedReport(0, 0xC0FFEE, [0x02, 0x01, 0x06]),
        ];

        List<IGapAdvertisement> heard = await ScanAsync(1, [.. tooLong, ExtendedReport(0, 0xBEEF, [0x02, 0x01, 0x06])]);

        heard.ShouldHaveSingleItem().Address.Value.ShouldBe((UInt48)0xBEEF);
    }
}
