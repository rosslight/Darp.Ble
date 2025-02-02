using System.Buffers.Binary;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using FluentAssertions;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataExtensionsSetterTests
{
    private static readonly AdvertisingData PowerLevelData = AdvertisingData.From(
    [
        (AdTypes.TxPowerLevel, [0x08]),
    ]);

    private static readonly BleUuid UuidInvalid = new((BleUuidType)1, Guid.Empty); // Invalid type
    private static readonly BleUuid Uuid16BitHeartRate = 0x180D; // 16-bit
    private static readonly BleUuid Uuid16BitDeviceInfo = 0x180A; // another 16-bit
    private static readonly BleUuid Uuid32 = BleUuid.FromUInt32(0xAABBCCDD); // 32-bit
    private static readonly BleUuid Uuid128BitCustom = BleUuid.FromGuid(Guid.Parse("12345678-1234-1234-1234-1234567890AB"));

    [Fact]
    public void WithFlags_EmptyAdvertisingData_AddsNewFlagsSection()
    {
        // Arrange
        var advertisingData = AdvertisingData.Empty;
        const AdvertisingDataFlags expectedFlags = AdvertisingDataFlags.GeneralDiscoverableMode;

        // Act
        AdvertisingData result = advertisingData.WithFlags(expectedFlags);

        // Assert
        result.Count.Should().Be(1, because: "one Flags section should be added to empty data");
        result.Contains(AdTypes.Flags).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.Flags);
        result[0].Bytes.Length.Should().Be(1);
        result[0].Bytes.Span[0].Should().Be((byte)expectedFlags);
    }

    [Fact]
    public void WithFlags_FlagsAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        const AdvertisingDataFlags existingFlags = AdvertisingDataFlags.LimitedDiscoverableMode;
        AdvertisingData advertisingData = AdvertisingData.From(
        [
            (AdTypes.Flags, [(byte)existingFlags]),
        ]);
        const AdvertisingDataFlags newFlags =
            AdvertisingDataFlags.GeneralDiscoverableMode | AdvertisingDataFlags.ClassicNotSupported;

        // Act
        AdvertisingData result = advertisingData.WithFlags(newFlags);

        // Assert
        result.Count.Should().Be(1, because: "the existing Flags section should be replaced, not appended");
        result.Contains(AdTypes.Flags).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.Flags);
        result[0].Bytes.Length.Should().Be(1);
        result[0].Bytes.Span[0].Should().Be((byte)newFlags);
    }

    [Fact]
    public void WithFlags_NoFlagsSection_AddsNewFlagsSection()
    {
        // Arrange
        const AdvertisingDataFlags flags = AdvertisingDataFlags.GeneralDiscoverableMode;

        // Act
        AdvertisingData result = PowerLevelData.WithFlags(flags);

        // Assert
        result.Count.Should().Be(2, because: "a new Flags section should be added alongside existing sections");
        result.Contains(AdTypes.TxPowerLevel).Should().BeTrue();
        result.Contains(AdTypes.Flags).Should().BeTrue();
        result[^1].Type.Should().Be(AdTypes.Flags);
        result[^1].Bytes.Length.Should().Be(1);
        result[^1].Bytes.Span[0].Should().Be((byte)flags);
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
        original.Count.Should().Be(1);
        original.Contains(AdTypes.Flags).Should().BeFalse();

        // Modified should have both TxPowerLevel and Flags
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.Flags).Should().BeTrue();
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
        result.Count.Should().Be(1, because: "one CompleteLocalName section should be added to empty data");
        result.Contains(AdTypes.CompleteLocalName).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.CompleteLocalName);
        // Verify the UTF-8 bytes match "MyDevice"
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.Should().Be(newName);
    }

    [Fact]
    public void WithCompleteLocalName_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.CompleteLocalName, "OldName"u8.ToArray()),
        ]);
        const string newName = "NewDeviceName";

        // Act
        AdvertisingData result = original.WithCompleteLocalName(newName);

        // Assert
        // The existing CompleteLocalName section should be replaced
        result.Count.Should().Be(1, because: "the existing CompleteLocalName section should be replaced, not appended");
        result.Contains(AdTypes.CompleteLocalName).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.CompleteLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.Should().Be(newName);
    }

    [Fact]
    public void WithCompleteLocalName_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.TxPowerLevel, [0x08]),
        ]);
        const string newName = "AnotherDevice";

        // Act
        AdvertisingData modified = original.WithCompleteLocalName(newName);

        // Assert
        // Original remains the same
        original.Count.Should().Be(1);
        original.Contains(AdTypes.CompleteLocalName).Should().BeFalse();

        // Modified now has an extra CompleteLocalName section
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.CompleteLocalName).Should().BeTrue();
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
        result.Count.Should().Be(1, because: "one ShortenedLocalName section should be added to empty data");
        result.Contains(AdTypes.ShortenedLocalName).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.ShortenedLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.Should().Be(shortName);
    }

    [Fact]
    public void WithShortenedLocalName_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.ShortenedLocalName, "OldShortName"u8.ToArray()),
        ]);
        const string newShortName = "NewShort";

        // Act
        AdvertisingData result = original.WithShortenedLocalName(newShortName);

        // Assert
        result.Count.Should()
            .Be(1, because: "the existing ShortenedLocalName section should be replaced, not appended");
        result.Contains(AdTypes.ShortenedLocalName).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.ShortenedLocalName);
        string resultName = Encoding.UTF8.GetString(result[0].Bytes.Span);
        resultName.Should().Be(newShortName);
    }

    [Fact]
    public void WithShortenedLocalName_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.Flags, [(byte)AdvertisingDataFlags.LimitedDiscoverableMode]),
        ]);
        const string newShortName = "MyShort";

        // Act
        AdvertisingData modified = original.WithShortenedLocalName(newShortName);

        // Assert
        // Original remains the same
        original.Count.Should().Be(1);
        original.Contains(AdTypes.ShortenedLocalName).Should().BeFalse();

        // Modified now has an extra ShortenedLocalName section
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.ShortenedLocalName).Should().BeTrue();
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
        result.Count.Should().Be(1, because: "one ManufacturerSpecificData section should be added to empty data");
        result.Contains(AdTypes.ManufacturerSpecificData).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.ManufacturerSpecificData);

        // Verify the content: 2 bytes for company ID + the original data
        result[0].Bytes.Length.Should().Be(2 + data.Length);
        ushort writtenCompanyId = BinaryPrimitives.ReadUInt16LittleEndian(result[0].Bytes.Span[..2]);
        writtenCompanyId.Should().Be((ushort)companyId);

        // Remaining part should match data
        ReadOnlyMemory<byte> readData = result[0].Bytes[2..];
        readData.Span.ToArray().Should().BeEquivalentTo(data.ToArray());
    }

    [Fact]
    public void WithManufacturerSpecificData_SectionAlreadyPresent_ReplacesExistingSection()
    {
        // Arrange
        const CompanyIdentifiers companyId = CompanyIdentifiers.NordicSemiconductorAsa;
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.ManufacturerSpecificData, [..BitConverter.GetBytes((ushort)companyId), 0x01, 0x02]),
        ]);
        ReadOnlyMemory<byte> newData = new byte[] { 0xAA, 0xBB, 0xCC };

        // Act
        AdvertisingData result = original.WithManufacturerSpecificData(companyId, newData.ToArray());

        // Assert
        // The existing ManufacturerSpecificData section should be replaced
        result.Count.Should().Be(1, because: "the existing ManufacturerSpecificData section should be replaced");
        result.Contains(AdTypes.ManufacturerSpecificData).Should().BeTrue();
        result[0].Type.Should().Be(AdTypes.ManufacturerSpecificData);

        // Verify the content
        ushort writtenCompanyId = BinaryPrimitives.ReadUInt16LittleEndian(result[0].Bytes.Span[..2]);
        writtenCompanyId.Should().Be((ushort)companyId);
        ReadOnlyMemory<byte> replacedData = result[0].Bytes[2..];
        replacedData.Span.ToArray().Should().BeEquivalentTo(newData.ToArray());
    }

    [Fact]
    public void WithManufacturerSpecificData_OriginalAdvertisingData_RemainsUnchanged()
    {
        // Arrange
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.CompleteLocalName, "MyPeripheral"u8.ToArray()),
        ]);
        const CompanyIdentifiers companyId = CompanyIdentifiers.SonyEricssonMobileCommunications;
        ReadOnlyMemory<byte> msData = new byte[] { 0x10, 0x20 };

        // Act
        AdvertisingData modified = original.WithManufacturerSpecificData(companyId, msData);

        // Assert
        // Original remains unchanged
        original.Count.Should().Be(1);
        original.Contains(AdTypes.ManufacturerSpecificData).Should().BeFalse();

        // Modified has an extra ManufacturerSpecificData section
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.ManufacturerSpecificData).Should().BeTrue();
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
        // Because we have a single 16-bit UUID, we should get exactly one new section:
        // AdTypes.CompleteListOf16BitServiceOrServiceClassUuids
        result.Count.Should().Be(1, "one Complete 16-bit Service UUID section should be added to empty data");
        result.Contains(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids).Should().BeTrue();

        // Verify the bytes are exactly 2 bytes of 0x180D
        result[0].Type.Should().Be(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.Should().Be(2);
        // 0x180D in little-endian is 0x0D, 0x18
        result[0].Bytes.Span[0].Should().Be(0x0D);
        result[0].Bytes.Span[1].Should().Be(0x18);
    }

    [Fact]
    public void WithCompleteListOfServiceUuids_MultipleUuids_ExistingSections_ReplacesThem()
    {
        // Arrange
        // Create original data with an existing complete 16-bit section
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [ 0x0D, 0x18 ]),
        ]);
        // We will add two 16-bit and one 128-bit
        BleUuid[] newUuids = [Uuid16BitHeartRate, Uuid16BitDeviceInfo, Uuid128BitCustom];

        // Act
        AdvertisingData result = original.WithCompleteListOfServiceUuids(newUuids);

        // Assert
        // We expect two new sections: one for 16-bit and one for 128-bit
        // (assuming none are 32-bit in this set)
        result.Count.Should().Be(2,
            "existing 16-bit section should be replaced, plus a new 128-bit section added");

        result[0].Type.Should().Be(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.Should().Be(4);
        result[0].Bytes.ToArray().Should().BeEquivalentTo([0x0D, 0x18, 0x0A, 0x18]);
        result[1].Type.Should().Be(AdTypes.CompleteListOf128BitServiceOrServiceClassUuids);
        result[1].Bytes.Length.Should().Be(16);
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
        original.Count.Should().Be(1);
        original.Contains(AdTypes.CompleteListOf16BitServiceOrServiceClassUuids).Should().BeFalse();

        // Modified now has 2 sections: Flags + CompleteListOf16Bit
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.CompleteListOf32BitServiceOrServiceClassUuids).Should().BeTrue();
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
        // Because we have a single 16-bit UUID, we should get exactly one new section:
        // AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids
        result.Count.Should().Be(1, "one Complete 16-bit Service UUID section should be added to empty data");
        result.Contains(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids).Should().BeTrue();

        // Verify the bytes are exactly 2 bytes of 0x180D
        result[0].Type.Should().Be(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.Should().Be(2);
        // 0x180D in little-endian is 0x0D, 0x18
        result[0].Bytes.Span[0].Should().Be(0x0D);
        result[0].Bytes.Span[1].Should().Be(0x18);
    }

    [Fact]
    public void WithIncompleteListOfServiceUuids_MultipleUuids_ExistingSections_ReplacesThem()
    {
        // Arrange
        // Create original data with an existing complete 16-bit section
        AdvertisingData original = AdvertisingData.From(
        [
            (AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids, [ 0x0D, 0x18 ]),
        ]);
        // We will add two 16-bit and one 128-bit
        BleUuid[] newUuids = [Uuid16BitHeartRate, Uuid16BitDeviceInfo, Uuid128BitCustom];

        // Act
        AdvertisingData result = original.WithIncompleteListOfServiceUuids(newUuids);

        // Assert
        // We expect two new sections: one for 16-bit and one for 128-bit
        // (assuming none are 32-bit in this set)
        result.Count.Should().Be(2,
            "existing 16-bit section should be replaced, plus a new 128-bit section added");

        result[0].Type.Should().Be(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        result[0].Bytes.Length.Should().Be(4);
        result[0].Bytes.ToArray().Should().BeEquivalentTo([0x0D, 0x18, 0x0A, 0x18]);
        result[1].Type.Should().Be(AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids);
        result[1].Bytes.Length.Should().Be(16);
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
        original.Count.Should().Be(1);
        original.Contains(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids).Should().BeFalse();

        // Modified now has 2 sections: Flags + IncompleteListOf16Bit
        modified.Count.Should().Be(2);
        modified.Contains(AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids).Should().BeTrue();
    }

    [Fact]
    public void WithCompleteListOfServiceUuids_InvalidUuid_ShouldThrow()
    {
        // Act
        Func<AdvertisingData> action = () => AdvertisingData.Empty.WithCompleteListOfServiceUuids(UuidInvalid);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}