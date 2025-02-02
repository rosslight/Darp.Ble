using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using FluentAssertions;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataExtensionsGetterTests
{
    private const string AdDataEmpty = "";
    private const string AdDataFlagEmpty = "0101";
    private const string AdDataFlagLimitedDiscoverable = "020101";
    private const string AdDataFlagsLimitedDiscoverableGeneralDiscoverable = "020101020102";
    private const string AdDataCompleteLocalNameEmpty = "0109";
    private const string AdDataCompleteLocalNameTestName = "0909546573744E616D65";
    private const string AdDataShortenedLocalNameEmpty = "0108";
    private const string AdDataShortenedLocalNameTestName = "0908546573744E616D65";
    private const string AdDataFlagsLimitedDiscoverableCompleteLocalNameTestName =
        "0201010909546573744E616D65";
    private const string AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName =
        "0201010908546573744E616D65";

    private const string AdDataIncomplete16Uuids0XAabb = "0302BBAA";
    private const string AdDataComplete16Uuids0XAabb = "0303BBAA";
    private const string AdDataComplete16Uuids0XAabbAacc = "0503BBAACCAA";
    private const string AdDataComplete32Uuids0XAabbccdd = "0505DDCCBBAA";
    private const string AdDataComplete32Uuids0XAabbccddAabbccee = "0905DDCCBBAAEECCBBAA";
    private const string AdDataComplete128Uuids = "1106FFEEDDCCBBAA99887766554433221100";
    private const string AdDataManufacturerSpecificInvalid = "02FF4C";
    private const string AdDataManufacturerSpecificApple = "07FF4C0012020002";

    [Theory]
    [InlineData(AdDataEmpty, false, AdvertisingDataFlags.None)]
    [InlineData(AdDataFlagEmpty, false, AdvertisingDataFlags.None)]
    [InlineData(AdDataFlagLimitedDiscoverable, true, AdvertisingDataFlags.LimitedDiscoverableMode)]
    [InlineData(
        AdDataFlagsLimitedDiscoverableGeneralDiscoverable,
        true,
        AdvertisingDataFlags.LimitedDiscoverableMode | AdvertisingDataFlags.GeneralDiscoverableMode
    )]
    public void TryGetFlags_WithFlagsPresent_ReturnsTrueAndFlags(
        string sections,
        bool expectedSuccess,
        AdvertisingDataFlags expectedFlags
    )
    {
        // Arrange
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        bool result = data.TryGetFlags(out AdvertisingDataFlags flags);

        // Assert
        result.Should().Be(expectedSuccess);
        flags.Should().Be(expectedFlags);
    }

    [Theory]
    [InlineData(AdDataEmpty, false, null)]
    [InlineData(AdDataFlagLimitedDiscoverable, false, null)]
    [InlineData(AdDataCompleteLocalNameTestName, false, null)]
    [InlineData(AdDataShortenedLocalNameEmpty, true, "")]
    [InlineData(AdDataShortenedLocalNameTestName, true, "TestName")]
    [InlineData(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName, true, "TestName")]
    public void TryGetShortenedLocalName_WithNamePresent_ReturnsTrueAndName(
        string sections,
        bool expectedSuccess,
        string? expectedName
    )
    {
        // Arrange
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        bool result = data.TryGetShortenedLocalName(out string? name);

        // Assert
        result.Should().Be(expectedSuccess);
        name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(AdDataEmpty, false, null)]
    [InlineData(AdDataFlagLimitedDiscoverable, false, null)]
    [InlineData(AdDataShortenedLocalNameEmpty, true, "")]
    [InlineData(AdDataCompleteLocalNameEmpty, true, "")]
    [InlineData(AdDataShortenedLocalNameTestName, true, "TestName")]
    [InlineData(AdDataCompleteLocalNameTestName, true, "TestName")]
    [InlineData(AdDataFlagsLimitedDiscoverableShortenedLocalNameTestName, true, "TestName")]
    [InlineData(AdDataFlagsLimitedDiscoverableCompleteLocalNameTestName, true, "TestName")]
    public void TryGetLocalName_WithNamePresent_ReturnsTrueAndName(
        string sections,
        bool expectedSuccess,
        string? expectedName
    )
    {
        // Arrange
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        bool result = data.TryGetLocalName(out string? name);

        // Assert
        result.Should().Be(expectedSuccess);
        name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(AdDataEmpty, BleUuidType.Uuid16)]
    [InlineData(AdDataFlagsLimitedDiscoverableGeneralDiscoverable, BleUuidType.Uuid16)]
    [InlineData(
        AdDataIncomplete16Uuids0XAabb,
        BleUuidType.Uuid16,
        "0000aabb-0000-1000-8000-00805f9b34fb"
    )]
    [InlineData(
        AdDataComplete16Uuids0XAabb,
        BleUuidType.Uuid16,
        "0000aabb-0000-1000-8000-00805f9b34fb"
    )]
    [InlineData(
        AdDataComplete16Uuids0XAabbAacc,
        BleUuidType.Uuid16,
        "0000aabb-0000-1000-8000-00805f9b34fb",
        "0000aacc-0000-1000-8000-00805f9b34fb"
    )]
    [InlineData(
        AdDataComplete32Uuids0XAabbccdd,
        BleUuidType.Uuid32,
        "aabbccdd-0000-1000-8000-00805f9b34fb"
    )]
    [InlineData(
        AdDataComplete32Uuids0XAabbccddAabbccee,
        BleUuidType.Uuid32,
        "aabbccdd-0000-1000-8000-00805f9b34fb",
        "aabbccee-0000-1000-8000-00805f9b34fb"
    )]
    [InlineData(
        AdDataComplete128Uuids,
        BleUuidType.Uuid128,
        "ccddeeff-aabb-8899-7766-554433221100"
    )]
    public void GetServices_WithMultipleServiceUuids_ReturnsUuids(
        string sections,
        BleUuidType type,
        params string[] guids
    )
    {
        // Arrange
        BleUuid[] expectedUuids = guids.Select(x => new BleUuid(type, Guid.Parse(x))).ToArray();
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        IEnumerable<BleUuid> result = data.GetServiceUuids();

        // Assert
        result.Should().BeEquivalentTo(expectedUuids);
    }

    [Theory]
    [InlineData(AdDataEmpty, false, (CompanyIdentifiers)0, "")]
    [InlineData(AdDataFlagLimitedDiscoverable, false, (CompanyIdentifiers)0, "")]
    [InlineData(AdDataManufacturerSpecificApple, true, CompanyIdentifiers.AppleInc, "12020002")]
    public void TryGetManufacturerSpecificData(
        string sections,
        bool expectedSuccess,
        CompanyIdentifiers? expectedCompanyIdentifiers,
        string expectedDataString
    )
    {
        // Arrange
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        bool result = data.TryGetManufacturerSpecificData(
            out CompanyIdentifiers companyUuid,
            out ReadOnlyMemory<byte> manufacturerData
        );

        // Assert
        result.Should().Be(expectedSuccess);
        companyUuid.Should().Be(expectedCompanyIdentifiers);
        manufacturerData
            .ToArray()
            .Should()
            .BeEquivalentTo(Convert.FromHexString(expectedDataString));
    }

    [Theory]
    [InlineData(AdDataEmpty, CompanyIdentifiers.AppleInc, false, "")]
    [InlineData(AdDataManufacturerSpecificInvalid, CompanyIdentifiers.AppleInc, false, "")]
    [InlineData(AdDataFlagLimitedDiscoverable, CompanyIdentifiers.AppleInc, false, "")]
    [InlineData(AdDataManufacturerSpecificApple, CompanyIdentifiers.Microsoft, false, "")]
    [InlineData(AdDataManufacturerSpecificApple, CompanyIdentifiers.AppleInc, true, "12020002")]
    public void TryGetManufacturerSpecificData_WithRequestedCompany(
        string sections,
        CompanyIdentifiers companyUuid,
        bool expectedSuccess,
        string expectedDataString
    )
    {
        // Arrange
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString(sections));

        // Act
        bool result = data.TryGetManufacturerSpecificData(
            companyUuid,
            out ReadOnlyMemory<byte> manufacturerData
        );

        // Assert
        result.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            manufacturerData.ToArray().Should().BeEquivalentTo(expectedDataString.ToByteArray());
        }
    }
}
