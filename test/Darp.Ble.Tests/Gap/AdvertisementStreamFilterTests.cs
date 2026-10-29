using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementStreamFilterTests
{
    private static readonly IGapAdvertisement HeartRateSensor = Create(
        0x01,
        BleEventType.AdvInd,
        AdvertisingData.From([(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, new byte[] { 0x0D, 0x18 })])
    );
    private static readonly IGapAdvertisement Beacon = Create(0x02, BleEventType.AdvNonConnInd, AdvertisingData.Empty);
    private static readonly IGapAdvertisement ScanResponse = Create(
        0x01,
        BleEventType.AdvInd | BleEventType.ScanResponse,
        AdvertisingData.Empty
    );

    private static readonly IGapAdvertisement[] All = [HeartRateSensor, Beacon, ScanResponse];

    [Fact]
    public async Task WhereService_KeepsAdvertisementsListingTheService()
    {
        IGapAdvertisement[] result = await All.ToAsyncEnumerable()
            .WhereService((ushort)0x180D)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        result.ShouldBe([HeartRateSensor]);
    }

    [Fact]
    public async Task WhereAddress_KeepsAdvertisementsFromTheAddress()
    {
        IGapAdvertisement[] result = await All.ToAsyncEnumerable()
            .WhereAddress(0x01UL)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        result.ShouldBe([HeartRateSensor, ScanResponse]);
    }

    [Fact]
    public async Task WhereAddress_WithAnAddressWithoutItsType_KeepsItsValueOfAnyType()
    {
        BleAddress parsed = BleAddress.Parse("00:00:00:00:00:01", provider: null);

        IGapAdvertisement[] result = await All.ToAsyncEnumerable()
            .WhereAddress(parsed)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        result.ShouldBe([HeartRateSensor, ScanResponse]);
    }

    [Fact]
    public async Task EventFilters_KeepTheirEventType()
    {
        CancellationToken token = TestContext.Current.CancellationToken;

        (await All.ToAsyncEnumerable().WhereConnectable().ToArrayAsync(token)).ShouldBe([
            HeartRateSensor,
            ScanResponse,
        ]);
        (await All.ToAsyncEnumerable().WhereScanResponse().ToArrayAsync(token)).ShouldBe([ScanResponse]);
        (await All.ToAsyncEnumerable().WhereType(BleEventType.AdvNonConnInd).ToArrayAsync(token)).ShouldBe([Beacon]);
    }

    [Fact]
    public async Task NullFilters_KeepEverything()
    {
        IGapAdvertisement[] result = await All.ToAsyncEnumerable()
            .WhereAddress((BleAddress?)null)
            .WhereService((BleUuid?)null)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(All);
    }

    [Fact]
    public async Task NullUserData_NeverMatchesATypedRequest()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        IGapAdvertisement[] withNull = [Beacon.WithUserData<string?>(null), Beacon.WithUserData<int?>(null)];

        (await withNull.ToAsyncEnumerable().OfUserData<string>().ToArrayAsync(token)).ShouldBeEmpty();
        (await withNull.ToAsyncEnumerable().OfUserData<int?>().ToArrayAsync(token)).ShouldBeEmpty();
        await Should.ThrowAsync<InvalidCastException>(() =>
            withNull.ToAsyncEnumerable().CastUserData<string>().ToArrayAsync(token).AsTask()
        );
    }

    private static GapAdvertisement Create(ulong address, BleEventType eventType, AdvertisingData data) =>
        GapAdvertisement.Create(
            DateTimeOffset.UnixEpoch,
            eventType,
            new BleAddress(BleAddressType.Public, (UInt48)address),
            Physical.Le1M,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            TxPowerLevel.NotAvailable,
            (Rssi)(-50),
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            BleAddress.NotAvailable,
            data
        );
}
