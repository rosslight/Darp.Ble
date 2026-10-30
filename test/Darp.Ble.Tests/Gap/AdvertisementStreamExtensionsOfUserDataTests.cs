using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementStreamExtensionsOfUserDataTests
{
    private interface ICreature;

    private class Animal;

    private sealed class Dog : Animal, ICreature;

    private const string ReportHex = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB";

    private static GapAdvertisement CreateBaseAdvertisement() =>
        GapAdvertisement.FromExtendedAdvertisingReport(DateTimeOffset.UtcNow, ReportHex.ToByteArray());

    [Fact]
    public async Task OfUserData_WhenAlreadyGeneric_ShouldPassThroughSameInstance()
    {
        IGapAdvertisement<string> adv = CreateBaseAdvertisement().WithUserData("abc");
        IGapAdvertisement<string> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<string>()
            .FirstAsync(TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(adv);
    }

    [Fact]
    public async Task OfUserData_WhenUserDataMatchesButGenericDifferent_ShouldWrapToRequestedGeneric()
    {
        // Create an advertisement with UserData typed as object but value is int
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(12345);

        IGapAdvertisement<int> result = await new[] { advWithObject }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<int>()
            .FirstAsync(TestContext.Current.CancellationToken);

        result.UserData.ShouldBe(12345);
        result.AsReadOnlyMemory().ShouldBe(advWithObject.AsReadOnlyMemory());
        result.Address.ShouldBe(advWithObject.Address);
        result.EventType.ShouldBe(advWithObject.EventType);
    }

    [Fact]
    public async Task OfUserData_WhenNoMatchingItems_ShouldNotEmit()
    {
        IGapAdvertisement<double> adv = CreateBaseAdvertisement().WithUserData(1.23);

        IGapAdvertisement<int>? result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<int>()
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task OfUserData_DerivedMatchesBase_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<Animal>? result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.UserData.ShouldBeAssignableTo<Dog>();
        result.AsReadOnlyMemory().ShouldBe(adv.AsReadOnlyMemory());
    }

    [Fact]
    public async Task OfUserData_InterfaceMatchesImplementation_ShouldWrapAndEmit()
    {
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        IGapAdvertisement<ICreature>? result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<ICreature>()
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.UserData.ShouldBeAssignableTo<Dog>();
    }

    [Fact]
    public async Task OfUserData_BaseDoesNotMatchDerived_ShouldNotEmit()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData(new Animal());

        IGapAdvertisement<Dog>? result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<Dog>()
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task OfUserData_AlreadyGenericBaseWithDerivedValue_ShouldPassThroughSameInstance()
    {
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData<Animal>(new Dog());

        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .OfUserData<Animal>()
            .FirstAsync(TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(adv);
        result.UserData.ShouldBeAssignableTo<Dog>();
    }

    [Theory]
    [InlineData(42)]
    public async Task CastUserData_WithNullableTypes_ShouldCastCorrectly(int? data)
    {
        // Arrange
        IGapAdvertisement<object?> adv = CreateBaseAdvertisement().WithUserData<object?>(data);

        // Act
        IGapAdvertisement<int?> result1 = await new[] { adv }
            .ToAsyncEnumerable()
            .OfUserData<int?>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result1.UserData.ShouldBe(data);
    }
}
