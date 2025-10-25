using System.Reactive.Linq;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using FluentAssertions;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementExtensionsOfUserDataTests
{
    private interface ICreature;

    private class Animal;

    private sealed class Dog : Animal, ICreature;

    private const string ReportHex = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB";

    private static GapAdvertisement CreateBaseAdvertisement() =>
        GapAdvertisement.FromExtendedAdvertisingReport(null!, DateTimeOffset.UtcNow, ReportHex.ToByteArray());

    [Fact]
    public async Task OfUserData_WhenAlreadyGeneric_ShouldPassThroughSameInstance()
    {
        IGapAdvertisement<string> adv = CreateBaseAdvertisement().WithUserData("abc");
        IGapAdvertisement<string> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<string>()
            .FirstAsync();

        result.Should().BeSameAs(adv);
    }

    [Fact]
    public async Task OfUserData_WhenUserDataMatchesButGenericDifferent_ShouldWrapToRequestedGeneric()
    {
        // Create an advertisement with UserData typed as object but value is int
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(12345);

        IGapAdvertisement<int> result = await new[] { advWithObject }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<int>()
            .FirstAsync();

        result.UserData.Should().Be(12345);
        result.AsByteArray().Should().BeEquivalentTo(advWithObject.AsByteArray());
        result.Address.Should().Be(advWithObject.Address);
        result.EventType.Should().Be(advWithObject.EventType);
    }

    [Fact]
    public async Task OfUserData_WhenNoMatchingItems_ShouldNotEmit()
    {
        IGapAdvertisement<double> adv = CreateBaseAdvertisement().WithUserData(1.23);

        IGapAdvertisement<int>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<int>()
            .FirstOrDefaultAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task OfUserData_DerivedMatchesBase_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<Animal>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstOrDefaultAsync();

        result.Should().NotBeNull();
        result!.UserData.Should().BeAssignableTo<Dog>();
        result.AsByteArray().Should().BeEquivalentTo(adv.AsByteArray());
    }

    [Fact]
    public async Task OfUserData_InterfaceMatchesImplementation_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<ICreature>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<ICreature>()
            .FirstOrDefaultAsync();

        result.Should().NotBeNull();
        result!.UserData.Should().BeAssignableTo<Dog>();
    }

    [Fact]
    public async Task OfUserData_BaseDoesNotMatchDerived_ShouldNotEmit()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData(new Animal());

        IGapAdvertisement<Dog>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Dog>()
            .FirstOrDefaultAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task OfUserData_AlreadyGenericBaseWithDerivedValue_ShouldPassThroughSameInstance()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData<Animal>(new Dog());

        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstAsync();

        result.Should().BeSameAs(adv);
        result.UserData.Should().BeAssignableTo<Dog>();
    }

    [Theory]
    [InlineData(42)]
    [InlineData(null)]
    public async Task CastUserData_WithNullableTypes_ShouldCastCorrectly(int? data)
    {
        // Arrange
        IGapAdvertisement<object?> adv = CreateBaseAdvertisement().WithUserData<object?>(data);

        // Act
        IGapAdvertisement<int?> result1 = await new[] { adv }.ToObservable().OfUserData<int?>().FirstAsync();

        // Assert
        result1.UserData.Should().Be(data);
    }
}
