using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using FluentAssertions;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataTests
{
    [Fact]
    public void From_WithByteArraySections_ReturnsValidData()
    {
        byte[] bytes = Convert.FromHexString("01");
        (AdTypes Section, byte[] Bytes)[] sectionsWithByteArray = [(AdTypes.Flags, bytes)];

        AdvertisingData data = AdvertisingData.From(sectionsWithByteArray);

        data[0].Type.Should().Be(AdTypes.Flags);
        data[0].Bytes.ToArray().Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void From_WithMemorySections_ReturnsValidData()
    {
        byte[] flagsBytes = Convert.FromHexString("01");
        byte[] manufacturerBytes = Convert.FromHexString("020304");
        byte[] serviceBytes = Convert.FromHexString("0506");
        (AdTypes Section, ReadOnlyMemory<byte> Bytes)[] sectionsWithByteArray =
        [
            (AdTypes.Flags, flagsBytes),
            (AdTypes.ManufacturerSpecificData, manufacturerBytes),
            (AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids, serviceBytes),
        ];

        AdvertisingData data = AdvertisingData.From(sectionsWithByteArray);

        data[0].Type.Should().Be(AdTypes.Flags);
        data[0].Bytes.ToArray().Should().BeEquivalentTo(flagsBytes);
        data[1].Type.Should().Be(AdTypes.ManufacturerSpecificData);
        data[1].Bytes.ToArray().Should().BeEquivalentTo(manufacturerBytes);
        data[2].Type.Should().Be(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        data[2].Bytes.ToArray().Should().BeEquivalentTo(serviceBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("00")]
    public void X(string hexString, params AdTypes[] sections)
    {
        AdvertisingData advertisingData = AdvertisingData.From(Convert.FromHexString(hexString));

        advertisingData.Should().HaveCount(sections.Length);
        advertisingData.Select(x => x.Type).Should().BeEquivalentTo(sections);
    }
}