using System.Reactive.Linq;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using Shouldly;

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

        result.ShouldBeSameAs(adv);
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

        result.UserData.ShouldBe(12345);
        result.AsByteArray().ShouldBe(advWithObject.AsByteArray());
        result.Address.ShouldBe(advWithObject.Address);
        result.EventType.ShouldBe(advWithObject.EventType);
    }

    [Fact]
    public async Task OfUserData_WhenNoMatchingItems_ShouldNotEmit()
    {
        IGapAdvertisement<double> adv = CreateBaseAdvertisement().WithUserData(1.23);

        IGapAdvertisement<int>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<int>()
            .FirstOrDefaultAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task OfUserData_DerivedMatchesBase_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<Animal>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstOrDefaultAsync();

        result.ShouldNotBeNull();
        result!.UserData.ShouldBeAssignableTo<Dog>();
        result.AsByteArray().ShouldBe(adv.AsByteArray());
    }

    [Fact]
    public async Task OfUserData_InterfaceMatchesImplementation_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<ICreature>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<ICreature>()
            .FirstOrDefaultAsync();

        result.ShouldNotBeNull();
        result!.UserData.ShouldBeAssignableTo<Dog>();
    }

    [Fact]
    public async Task OfUserData_BaseDoesNotMatchDerived_ShouldNotEmit()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData(new Animal());

        IGapAdvertisement<Dog>? result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Dog>()
            .FirstOrDefaultAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task OfUserData_AlreadyGenericBaseWithDerivedValue_ShouldPassThroughSameInstance()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData<Animal>(new Dog());

        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstAsync();

        result.ShouldBeSameAs(adv);
        result.UserData.ShouldBeAssignableTo<Dog>();
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
        result1.UserData.ShouldBe(data);
    }
}
