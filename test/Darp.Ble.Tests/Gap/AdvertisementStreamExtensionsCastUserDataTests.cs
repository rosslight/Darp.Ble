using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Shouldly;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementStreamExtensionsCastUserDataTests
{
    private interface ICreature;

    private class Animal;

    private sealed class Dog : Animal, ICreature;

    private static GapAdvertisement CreateBaseAdvertisement() =>
        GapAdvertisementTests.Create(AdvertisingData.From([(AdTypes.Flags, [0x1A])]));

    [Fact]
    public async Task CastUserData_WhenAlreadyGeneric_ShouldPassThroughSameInstance()
    {
        // Arrange
        IGapAdvertisement<string> adv = CreateBaseAdvertisement().WithUserData("abc");

        // Act
        IGapAdvertisement<string> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<string>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(adv);
    }

    [Fact]
    public async Task CastUserData_WhenUserDataMatchesButGenericDifferent_ShouldWrapToRequestedGeneric()
    {
        // Arrange
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(12345);

        // Act
        IGapAdvertisement<int> result = await new[] { advWithObject }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<int>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.UserData.ShouldBe(12345);
        result.Data.ShouldBe(advWithObject.Data);
        result.Address.ShouldBe(advWithObject.Address);
        result.EventType.ShouldBe(advWithObject.EventType);
    }

    [Fact]
    public async Task CastUserData_WhenUserDataIsNull_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<object?> advWithNull = CreateBaseAdvertisement().WithUserData<object?>(null);

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithNull }
                .ToAsyncEnumerable<IGapAdvertisement>()
                .CastUserData<int>()
                .FirstAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_WhenUserDataCannotBeCast_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<string> advWithString = CreateBaseAdvertisement().WithUserData("not a number");

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithString }
                .ToAsyncEnumerable<IGapAdvertisement>()
                .CastUserData<int>()
                .FirstAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_WhenUserDataIsIncompatibleType_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<double> advWithDouble = CreateBaseAdvertisement().WithUserData(1.23);

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithDouble }
                .ToAsyncEnumerable<IGapAdvertisement>()
                .CastUserData<int>()
                .FirstAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_WhenAdvertisementHasNoUserData_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement advWithoutUserData = CreateBaseAdvertisement();

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithoutUserData }
                .ToAsyncEnumerable()
                .CastUserData<int>()
                .FirstAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_DerivedMatchesBase_ShouldCastAndEmit()
    {
        // Arrange
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        // Act
        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<Animal>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.UserData.ShouldBeAssignableTo<Dog>();
        result.Data.ShouldBe(adv.Data);
    }

    [Fact]
    public async Task CastUserData_InterfaceMatchesImplementation_ShouldCastAndEmit()
    {
        // Arrange
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        // Act
        IGapAdvertisement<ICreature> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<ICreature>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.UserData.ShouldBeAssignableTo<Dog>();
    }

    [Fact]
    public async Task CastUserData_BaseDoesNotMatchDerived_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData(new Animal());

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { adv }
                .ToAsyncEnumerable<IGapAdvertisement>()
                .CastUserData<Dog>()
                .FirstAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_AlreadyGenericBaseWithDerivedValue_ShouldPassThroughSameInstance()
    {
        // Arrange
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData<Animal>(new Dog());

        // Act
        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<Animal>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(adv);
        result.UserData.ShouldBeAssignableTo<Dog>();
    }

    [Fact]
    public async Task CastUserData_WithMultipleAdvertisements_ShouldProcessEachIndividually()
    {
        // Arrange
        IGapAdvertisement<string> adv1 = CreateBaseAdvertisement().WithUserData("test1");
        IGapAdvertisement<int> adv2 = CreateBaseAdvertisement().WithUserData(42);
        IGapAdvertisement<double> adv3 = CreateBaseAdvertisement().WithUserData(3.14);

        // Act
        var results = await new IGapAdvertisement[] { adv1, adv2, adv3 }
            .ToAsyncEnumerable()
            .CastUserData<object>()
            .ToArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Length.ShouldBe(3);
        results[0].UserData.ShouldBe("test1");
        results[1].UserData.ShouldBe(42);
        results[2].UserData.ShouldBe(3.14);
    }

    [Fact]
    public async Task CastUserData_WithMixedValidAndInvalid_ShouldThrowOnFirstInvalid()
    {
        // Arrange
        IGapAdvertisement<string> validAdv = CreateBaseAdvertisement().WithUserData("valid");
        IGapAdvertisement<int> invalidAdv = CreateBaseAdvertisement().WithUserData(123);

        // Act & Assert
        Func<Task> act = async () =>
            await new IGapAdvertisement[] { validAdv, invalidAdv }
                .ToAsyncEnumerable()
                .CastUserData<string>()
                .ToArrayAsync(TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CastUserData_WithValueTypes_ShouldCastCorrectly()
    {
        // Arrange
        IGapAdvertisement<object> advWithInt = CreateBaseAdvertisement().WithUserData<object>(42);
        IGapAdvertisement<object> advWithBool = CreateBaseAdvertisement().WithUserData<object>(true);
        IGapAdvertisement<object> advWithChar = CreateBaseAdvertisement().WithUserData<object>('A');

        // Act
        var intResult = await new[] { advWithInt }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<int>()
            .FirstAsync(TestContext.Current.CancellationToken);
        var boolResult = await new[] { advWithBool }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<bool>()
            .FirstAsync(TestContext.Current.CancellationToken);
        var charResult = await new[] { advWithChar }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<char>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        intResult.UserData.ShouldBe(42);
        boolResult.UserData.ShouldBe(true);
        charResult.UserData.ShouldBe('A');
    }

    [Fact]
    public async Task CastUserData_WithReferenceTypes_ShouldCastCorrectly()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(testObject);

        // Act
        var result = await new[] { advWithObject }
            .ToAsyncEnumerable<IGapAdvertisement>()
            .CastUserData<object>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result.UserData.ShouldBeSameAs(testObject);
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
            .CastUserData<int?>()
            .FirstAsync(TestContext.Current.CancellationToken);

        // Assert
        result1.UserData.ShouldBe(data);
    }
}
