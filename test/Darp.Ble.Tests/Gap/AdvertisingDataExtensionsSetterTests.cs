using System.Buffers.Binary;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataExtensionsSetterTests
{
    private static readonly AdvertisingData PowerLevelData = AdvertisingData.From([(AdTypes.TxPowerLevel, [0x08])]);

    private static readonly BleUuid Uuid16BitHeartRate = 0x180D; // 16-bit
    private static readonly BleUuid Uuid16BitDeviceInfo = 0x180A; // another 16-bit
    private static readonly BleUuid Uuid32 = BleUuid.FromUInt32(0xAABBCCDD); // 32-bit
    private static readonly BleUuid Uuid128BitCustom = BleUuid.FromGuid(
        Guid.Parse("12345678-1234-1234-1234-1234567890AB")
    );

    [Fact]
    public void WithFlags_EmptyAdvertisingData_AddsNewFlagsSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;
        const AdvertisingDataFlags expectedFlags = AdvertisingDataFlags.GeneralDiscoverableMode;

        // Act
        AdvertisingData result = advertisingData.WithFlags(expectedFlags);

        // Assert
        result.Count.ShouldBe(1, customMessage: "one Flags section should be added to empty data");
        result.Contains(AdTypes.Flags).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.Flags);
        result[0].Bytes.Length.ShouldBe(1);
        result[0].Bytes.Span[0].ShouldBe((byte)expectedFlags);
    }

    [Fact]
    public void WithFlags_FlagsAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        const AdvertisingDataFlags existingFlags = AdvertisingDataFlags.LimitedDiscoverableMode;
        AdvertisingData advertisingData = AdvertisingData.From([(AdTypes.Flags, [(byte)existingFlags])]);
        const AdvertisingDataFlags newFlags =
            AdvertisingDataFlags.GeneralDiscoverableMode | AdvertisingDataFlags.ClassicNotSupported;

        // Act
        AdvertisingData result = advertisingData.WithFlags(newFlags);

        // Assert
        result.Count.ShouldBe(1, customMessage: "the existing Flags section should be replaced, not appended");
        result.Contains(AdTypes.Flags).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.Flags);
        result[0].Bytes.Length.ShouldBe(1);
        result[0].Bytes.Span[0].ShouldBe((byte)newFlags);
    }

    [Fact]
    public void WithFlags_NoFlagsSection_AddsNewFlagsSection()
    {
        // Arrange
        const AdvertisingDataFlags flags = AdvertisingDataFlags.GeneralDiscoverableMode;

        // Act
        AdvertisingData result = PowerLevelData.WithFlags(flags);

        // Assert
        result.Count.ShouldBe(2, customMessage: "a new Flags section should be added alongside existing sections");
        result.Contains(AdTypes.TxPowerLevel).ShouldBeTrue();
        result.Contains(AdTypes.Flags).ShouldBeTrue();
        result[^1].Type.ShouldBe(AdTypes.Flags);
        result[^1].Bytes.Length.ShouldBe(1);
        result[^1].Bytes.Span[0].ShouldBe((byte)flags);
    }

    [Fact]
    public void WithFlags_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([(AdTypes.TxPowerLevel, [0x08])]);

        // Act
        AdvertisingData modified = original.WithFlags(AdvertisingDataFlags.LimitedDiscoverableMode);

        // Assert
        // Original should remain unchanged
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.Flags).ShouldBeFalse();

        // Modified should have both TxPowerLevel and Flags
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.Flags).ShouldBeTrue();
    }

    [Fact]
    public void WithCompleteLocalName_EmptyAdvertisingData_AddsNewCompleteLocalNameSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;
        const string newName = "MyDevice";

        // Act
        AdvertisingData result = advertisingData.WithCompleteLocalName(newName);

        // Assert
        result.Count.ShouldBe(1, customMessage: "one CompleteLocalName section should be added to empty data");
        result.Contains(AdTypes.CompleteLocalName).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.CompleteLocalName);
        // Verify the UTF-8 bytes match "MyDevice"
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.ShouldBe(newName);
    }

    [Fact]
    public void WithCompleteLocalName_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([(AdTypes.CompleteLocalName, "OldName"u8.ToArray())]);
        const string newName = "NewDeviceName";

        // Act
        AdvertisingData result = original.WithCompleteLocalName(newName);

        // Assert
        // The existing CompleteLocalName section should be replaced
        result.Count.ShouldBe(
            1,
            customMessage: "the existing CompleteLocalName section should be replaced, not appended"
        );
        result.Contains(AdTypes.CompleteLocalName).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.CompleteLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.ShouldBe(newName);
    }

    [Fact]
    public void WithCompleteLocalName_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([(AdTypes.TxPowerLevel, [0x08])]);
        const string newName = "AnotherDevice";

        // Act
        AdvertisingData modified = original.WithCompleteLocalName(newName);

        // Assert
        // Original remains the same
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.CompleteLocalName).ShouldBeFalse();

        // Modified now has an extra CompleteLocalName section
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.CompleteLocalName).ShouldBeTrue();
    }

    [Fact]
    public void WithShortenedLocalName_EmptyAdvertisingData_AddsNewShortenedLocalNameSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;
        const string shortName = "Dev";

        // Act
        AdvertisingData result = advertisingData.WithShortenedLocalName(shortName);

        // Assert
        result.Count.ShouldBe(1, customMessage: "one ShortenedLocalName section should be added to empty data");
        result.Contains(AdTypes.ShortenedLocalName).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.ShortenedLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.ShouldBe(shortName);
    }

    [Fact]
    public void WithShortenedLocalName_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([(AdTypes.ShortenedLocalName, "OldShortName"u8.ToArray())]);
        const string newShortName = "NewShort";

        // Act
        AdvertisingData result = original.WithShortenedLocalName(newShortName);

        // Assert
        result.Count.ShouldBe(
            1,
            customMessage: "the existing ShortenedLocalName section should be replaced, not appended"
        );
        result.Contains(AdTypes.ShortenedLocalName).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.ShortenedLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.ShouldBe(newShortName);
    }

    [Fact]
    public void WithShortenedLocalName_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.Flags, [(byte)AdvertisingDataFlags.LimitedDiscoverableMode]),
        ]);
        const string newShortName = "MyShort";

        // Act
        AdvertisingData modified = original.WithShortenedLocalName(newShortName);

        // Assert
        // Original remains the same
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.ShortenedLocalName).ShouldBeFalse();

        // Modified now has an extra ShortenedLocalName section
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.ShortenedLocalName).ShouldBeTrue();
    }

    [Fact]
    public void WithManufacturerSpecificData_EmptyAdvertisingData_AddsNewManufacturerSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;
        const CompanyIdentifiers companyId = CompanyIdentifiers.AppleInc;
        ReadOnlyMemory<byte> data = new byte[] { 0xAA, 0xBB, 0xCC };

        // Act
        AdvertisingData result = advertisingData.WithManufacturerSpecificData(companyId, [0xAA, 0xBB, 0xCC]);

        // Assert
        result.Count.ShouldBe(1, customMessage: "one ManufacturerSpecificData section should be added to empty data");
        result.Contains(AdTypes.ManufacturerSpecificData).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.ManufacturerSpecificData);

        // Verify the content: 2 bytes for company ID + the original data
        result[0].Bytes.Length.ShouldBe(2 + data.Length);
        ushort writtenCompanyId = BinaryPrimitives.ReadUInt16LittleEndian(result[0].Bytes.Span[..2]);
        writtenCompanyId.ShouldBe((ushort)companyId);

        // Remaining part should match data
        ReadOnlyMemory<byte> readData = result[0].Bytes[2..];
        readData.Span.ToArray().ShouldBe(data.ToArray());
    }

    [Fact]
    public void WithManufacturerSpecificData_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        const CompanyIdentifiers companyId = CompanyIdentifiers.NordicSemiconductorAsa;
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.ManufacturerSpecificData, [.. BitConverter.GetBytes((ushort)companyId), 0x01, 0x02]),
        ]);
        ReadOnlyMemory<byte> newData = new byte[] { 0xAA, 0xBB, 0xCC };

        // Act
        AdvertisingData result = original.WithManufacturerSpecificData(companyId, newData.ToArray());

        // Assert
        // The existing ManufacturerSpecificData section should be replaced
        result.Count.ShouldBe(1, customMessage: "the existing ManufacturerSpecificData section should be replaced");
        result.Contains(AdTypes.ManufacturerSpecificData).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.ManufacturerSpecificData);

        // Verify the content
        ushort writtenCompanyId = BinaryPrimitives.ReadUInt16LittleEndian(result[0].Bytes.Span[..2]);
        writtenCompanyId.ShouldBe((ushort)companyId);
        ReadOnlyMemory<byte> replacedData = result[0].Bytes[2..];
        replacedData.Span.ToArray().ShouldBe(newData.ToArray());
    }

    [Fact]
    public void WithManufacturerSpecificData_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([(AdTypes.CompleteLocalName, "MyPeripheral"u8.ToArray())]);
        const CompanyIdentifiers companyId = CompanyIdentifiers.SonyEricssonMobileCommunications;
        ReadOnlyMemory<byte> msData = new byte[] { 0x10, 0x20 };

        // Act
        AdvertisingData modified = original.WithManufacturerSpecificData(companyId, msData);

        // Assert
        // Original remains unchanged
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.ManufacturerSpecificData).ShouldBeFalse();

        // Modified has an extra ManufacturerSpecificData section
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.ManufacturerSpecificData).ShouldBeTrue();
    }

    [Fact]
    public void WithCompleteListOfServiceUuids_SingleUuid_EmptyAdvertisingData_Adds16BitSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;

        // Act
        // Single UUID version
        AdvertisingData result = advertisingData.WithCompleteListOfServiceUuids(Uuid16BitHeartRate);

        // Assert
        // customMessage we have a single 16-bit UUID, we should get exactly one new section:
        // AdTypes.CompleteListOf16BitServiceOrServiceClassUuids
        result.Count.ShouldBe(1, "one Complete 16-bit Service UUID section should be added to empty data");
        result.Contains(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids).ShouldBeTrue();

        // Verify the bytes are exactly 2 bytes of 0x180D
        result[0].Type.ShouldBe(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.ShouldBe(2);
        // 0x180D in little-endian is 0x0D, 0x18
        result[0].Bytes.Span[0].ShouldBe<byte>(0x0D);
        result[0].Bytes.Span[1].ShouldBe<byte>(0x18);
    }

    [Fact]
    public void WithCompleteListOfServiceUuids_MultipleUuids_ExistingSections_ReplacesThem()
    {
        // Arrange
        // Create original data with an existing complete 16-bit section
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [0x0D, 0x18]),
        ]);
        // We will add two 16-bit and one 128-bit
        BleUuid[] newUuids = [Uuid16BitHeartRate, Uuid16BitDeviceInfo, Uuid128BitCustom];

        // Act
        AdvertisingData result = original.WithCompleteListOfServiceUuids(newUuids);

        // Assert
        // We expect two new sections: one for 16-bit and one for 128-bit
        // (assuming none are 32-bit in this set)
        result.Count.ShouldBe(2, "existing 16-bit section should be replaced, plus a new 128-bit section added");

        result[0].Type.ShouldBe(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.ShouldBe(4);
        result[0].Bytes.ToArray().ShouldBe([0x0D, 0x18, 0x0A, 0x18]);
        result[1].Type.ShouldBe(AdTypes.CompleteListOf128BitServiceOrServiceClassUuids);
        result[1].Bytes.Length.ShouldBe(16);
    }

    [Fact]
    public void WithCompleteListOfServiceUuids_OriginalData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.Flags, [(byte)AdvertisingDataFlags.LimitedDiscoverableMode]),
        ]);

        // Act
        AdvertisingData modified = original.WithCompleteListOfServiceUuids(Uuid32);

        // Assert
        // Original remains the same
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids).ShouldBeFalse();

        // Modified now has 2 sections: Flags + CompleteListOf16Bit
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.CompleteListOf32BitServiceOrServiceClassUuids).ShouldBeTrue();
    }

    [Fact]
    public void WithIncompleteListOfServiceUuids_SingleUuid_EmptyAdvertisingData_Adds16BitSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;

        // Act
        // Single UUID version
        AdvertisingData result = advertisingData.WithIncompleteListOfServiceUuids(Uuid16BitHeartRate);

        // Assert
        // customMessage we have a single 16-bit UUID, we should get exactly one new section:
        // AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids
        result.Count.ShouldBe(1, "one Complete 16-bit Service UUID section should be added to empty data");
        result.Contains(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids).ShouldBeTrue();

        // Verify the bytes are exactly 2 bytes of 0x180D
        result[0].Type.ShouldBe(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.ShouldBe(2);
        // 0x180D in little-endian is 0x0D, 0x18
        result[0].Bytes.Span[0].ShouldBe<byte>(0x0D);
        result[0].Bytes.Span[1].ShouldBe<byte>(0x18);
    }

    [Fact]
    public void WithIncompleteListOfServiceUuids_MultipleUuids_ExistingSections_ReplacesThem()
    {
        // Arrange
        // Create original data with an existing complete 16-bit section
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids, [0x0D, 0x18]),
        ]);
        // We will add two 16-bit and one 128-bit
        BleUuid[] newUuids = [Uuid16BitHeartRate, Uuid16BitDeviceInfo, Uuid128BitCustom];

        // Act
        AdvertisingData result = original.WithIncompleteListOfServiceUuids(newUuids);

        // Assert
        // We expect two new sections: one for 16-bit and one for 128-bit
        // (assuming none are 32-bit in this set)
        result.Count.ShouldBe(2, "existing 16-bit section should be replaced, plus a new 128-bit section added");

        result[0].Type.ShouldBe(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.ShouldBe(4);
        result[0].Bytes.ToArray().ShouldBe([0x0D, 0x18, 0x0A, 0x18]);
        result[1].Type.ShouldBe(AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids);
        result[1].Bytes.Length.ShouldBe(16);
    }

    [Fact]
    public void WithIncompleteListOfServiceUuids_OriginalData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From([
            (AdTypes.Flags, [(byte)AdvertisingDataFlags.LimitedDiscoverableMode]),
        ]);

        // Act
        AdvertisingData modified = original.WithIncompleteListOfServiceUuids(Uuid32);

        // Assert
        // Original remains the same
        original.Count.ShouldBe(1);
        original.Contains(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids).ShouldBeFalse();

        // Modified now has 2 sections: Flags + IncompleteListOf16Bit
        modified.Count.ShouldBe(2);
        modified.Contains(AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids).ShouldBeTrue();
    }

    [Fact]
    public void WithTxPower_EmptyAdvertisingData_AddsNewTxPowerSection()
    {
        const TxPowerLevel expected = (TxPowerLevel)1;
        var advertisingData = AdvertisingData.Empty;

        AdvertisingData result = advertisingData.WithTxPower(expected);

        result.Count.ShouldBe(1, "one TxPowerLevel section should be added");
        result.Contains(AdTypes.TxPowerLevel).ShouldBeTrue();
        result[0].Type.ShouldBe(AdTypes.TxPowerLevel);
        result[0].Bytes.Span[0].ShouldBe((byte)expected);
    }

    [Fact]
    public void WithTxPower_SectionAlreadyPresent_ReplacesExistingSection()
    {
        var original = AdvertisingData.From([(AdTypes.TxPowerLevel, [0x05])]);
        TxPowerLevel newLevel = (TxPowerLevel)8;

        AdvertisingData result = original.WithTxPower(newLevel);

        result.Count.ShouldBe(1, "existing TxPowerLevel should be replaced");
        result[0].Bytes.Span[0].ShouldBe((byte)newLevel);
    }

    [Fact]
    public void WithTxPower_OriginalAdvertisingData_RemainsUnchanged()
    {
        var original = AdvertisingData.Empty;
        AdvertisingData modified = original.WithTxPower((TxPowerLevel)1);

        original.Count.ShouldBe(0);
        modified.Count.ShouldBe(1);
    }

    // New tests for WithServiceData
    [Fact]
    public void WithServiceData_16BitUuid_AddsServiceData16BitSection()
    {
        var advertisingData = AdvertisingData.Empty;
        byte[] svcData = [0xAA, 0xBB];

        AdvertisingData result = advertisingData.WithServiceData(Uuid16BitHeartRate, svcData);

        result.Count.ShouldBe(1);
        result[0].Type.ShouldBe(AdTypes.ServiceData16BitUuid);
        // first two bytes are the UUID in little-endian
        result[0].Bytes.Span[0].ShouldBe<byte>(0x0D);
        result[0].Bytes.Span[1].ShouldBe<byte>(0x18);
        // then service data
        result[0].Bytes.Span[2].ShouldBe<byte>(0xAA);
        result[0].Bytes.Span[3].ShouldBe<byte>(0xBB);
    }

    [Fact]
    public void WithServiceData_32BitUuid_AddsServiceData32BitSection()
    {
        var advertisingData = AdvertisingData.Empty;
        byte[] svcData = [0x01, 0x02, 0x03];

        AdvertisingData result = advertisingData.WithServiceData(Uuid32, svcData);

        result.Count.ShouldBe(1);
        result[0].Type.ShouldBe(AdTypes.ServiceData32BitUuid);
        // first 4 bytes are the UUID
        var span = result[0].Bytes.Span;
        span[0].ShouldBe<byte>(0xDD);
        span[1].ShouldBe<byte>(0xCC);
        span[2].ShouldBe<byte>(0xBB);
        span[3].ShouldBe<byte>(0xAA);
        span[4].ShouldBe<byte>(0x01);
    }

    [Fact]
    public void WithServiceData_128BitUuid_AddsServiceData128BitSection()
    {
        var advertisingData = AdvertisingData.Empty;
        byte[] svcData = [0xFF];

        AdvertisingData result = advertisingData.WithServiceData(Uuid128BitCustom, svcData);

        result.Count.ShouldBe(1);
        result[0].Type.ShouldBe(AdTypes.ServiceData128BitUuid);
        result[0].Bytes.Length.ShouldBe(16 + 1);
    }

    [Fact]
    public void WithServiceData_NullUuid_Throws()
    {
        byte[] svcData = [0x00];
        Func<AdvertisingData> act = () => AdvertisingData.Empty.WithServiceData(null!, svcData);
        act.ShouldThrow<ArgumentNullException>();
    }
}
