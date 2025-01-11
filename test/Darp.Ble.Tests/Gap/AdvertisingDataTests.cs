using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataTests
{
    public void WithCompleteLocalName()
    {
        AdvertisingData data = null!;
        var newData = data.WithCompleteLocalName("");
        newData = data.WithShortenedLocalName("");
        data.TryGetLocalName(out string? name1);
        data.TryGetShortenedLocalName(out string? name2);
        data.TryGetCompleteLocalName(out string? name3);
        newData = data.WithFlags(AdvertisingDataFlags.None);
        data.TryGetFlags(out var flags);
        data.WithIncompleteListOfServiceUuids(new BleUuid(0x1234), new BleUuid(0x1234), new BleUuid(0x1234));
        data.WithCompleteListOfServiceUuids(new BleUuid(0x1234));
        data.GetServiceUuids();
        data.WithManufacturerSpecificData(CompanyIdentifiers.AppleInc, "123"u8.ToArray());
        data.TryGetManufacturerSpecificData(out var company, out var d);
        // data.WithPowerLevel(TxPowerLevel.NotAvailable);
        // data.TryGetPowerLevel();
        // data.WithServiceData(new BleUuid(0x1234), ""u8.ToArray());
        // data.WithApperance();
    }
}