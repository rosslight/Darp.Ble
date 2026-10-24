using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.WinRT.Gap;
using Shouldly;

namespace Darp.Ble.WinRT.Tests;

/// <summary> The data sections Windows' publisher takes from the app </summary>
public sealed class PublisherSectionsTests
{
    [Fact]
    public void SectionsWindowsSetsItself_AreLeftOut_AndNamed()
    {
        AdvertisingData data = AdvertisingData.From(Convert.FromHexString("020106" + "05FF4C001234" + "0409446172"));

        (IReadOnlyList<(AdTypes Type, ReadOnlyMemory<byte> Bytes)> kept, IReadOnlyList<AdTypes> dropped) =
            PublisherSections.Split(data);

        kept.Select(section => section.Type).ShouldBe([AdTypes.ManufacturerSpecificData]);
        dropped.ShouldBe([AdTypes.Flags, AdTypes.CompleteLocalName]);
    }
}
