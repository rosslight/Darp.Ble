using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisingDataTests
{
    [Fact]
    public void From_WithByteArraySections_ReturnsValidData()
    {
        byte[] bytes = Convert.FromHexString("01");
        (AdTypes Section, byte[] Bytes)[] sectionsWithByteArray = [(AdTypes.Flags, bytes)];

        AdvertisingData data = AdvertisingData.From(sectionsWithByteArray);

        data[0].Type.ShouldBe(AdTypes.Flags);
        data[0].Bytes.ToArray().ShouldBe(bytes);
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

        data[0].Type.ShouldBe(AdTypes.Flags);
        data[0].Bytes.ToArray().ShouldBe(flagsBytes);
        data[1].Type.ShouldBe(AdTypes.ManufacturerSpecificData);
        data[1].Bytes.ToArray().ShouldBe(manufacturerBytes);
        data[2].Type.ShouldBe(AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids);
        data[2].Bytes.ToArray().ShouldBe(serviceBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("00")]
    public void X(string hexString, params AdTypes[] sections)
    {
        AdvertisingData advertisingData = AdvertisingData.From(Convert.FromHexString(hexString));

        advertisingData.Count.ShouldBe(sections.Length);
        advertisingData.Select(x => x.Type).ShouldBe(sections);
    }
}
