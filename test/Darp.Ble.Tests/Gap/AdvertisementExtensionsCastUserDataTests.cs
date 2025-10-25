using System.Reactive.Linq;
using Darp.Ble.Gap;
using Darp.Ble.Linq;
using FluentAssertions;

namespace Darp.Ble.Tests.Gap;

public sealed class AdvertisementExtensionsCastUserDataTests
{
    private interface ICreature;

    private class Animal;

    private sealed class Dog : Animal, ICreature;

    private const string ReportHex = "130000FFEEDDCCBBAA0100FF7FD80000FF0000000000000702011A0303AABB";

    private static GapAdvertisement CreateBaseAdvertisement() =>
        GapAdvertisement.FromExtendedAdvertisingReport(null!, DateTimeOffset.UtcNow, ReportHex.ToByteArray());

    [Fact]
    public async Task CastUserData_WhenAlreadyGeneric_ShouldPassThroughSameInstance()
    {
        // Arrange
        IGapAdvertisement<string> adv = CreateBaseAdvertisement().WithUserData("abc");

        // Act
        IGapAdvertisement<string> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<string>()
            .FirstAsync();

        // Assert
        result.Should().BeSameAs(adv);
    }

    [Fact]
    public async Task CastUserData_WhenUserDataMatchesButGenericDifferent_ShouldWrapToRequestedGeneric()
    {
        // Arrange
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(12345);

        // Act
        IGapAdvertisement<int> result = await new[] { advWithObject }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<int>()
            .FirstAsync();

        // Assert
        result.UserData.Should().Be(12345);
        result.AsByteArray().Should().BeEquivalentTo(advWithObject.AsByteArray());
        result.Address.Should().Be(advWithObject.Address);
        result.EventType.Should().Be(advWithObject.EventType);
    }

    [Fact]
    public async Task CastUserData_WhenUserDataIsNull_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<object?> advWithNull = CreateBaseAdvertisement().WithUserData<object?>(null);

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithNull }.ToObservable<IGapAdvertisement>().CastUserData<int>().FirstAsync();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement`1' to 'Int32'.");
    }

    [Fact]
    public async Task CastUserData_WhenUserDataCannotBeCast_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<string> advWithString = CreateBaseAdvertisement().WithUserData("not a number");

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithString }.ToObservable<IGapAdvertisement>().CastUserData<int>().FirstAsync();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement`1' to 'Int32'.");
    }

    [Fact]
    public async Task CastUserData_WhenUserDataIsIncompatibleType_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<double> advWithDouble = CreateBaseAdvertisement().WithUserData(1.23);

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { advWithDouble }.ToObservable<IGapAdvertisement>().CastUserData<int>().FirstAsync();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement`1' to 'Int32'.");
    }

    [Fact]
    public async Task CastUserData_WhenAdvertisementHasNoUserData_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement advWithoutUserData = CreateBaseAdvertisement();

        // Act & Assert
        Func<Task> act = async () => await new[] { advWithoutUserData }.ToObservable().CastUserData<int>().FirstAsync();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement' to 'Int32'.");
    }

    [Fact]
    public async Task CastUserData_DerivedMatchesBase_ShouldCastAndEmit()
    {
        // Arrange
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        // Act
        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<Animal>()
            .FirstAsync();

        // Assert
        result.UserData.Should().BeAssignableTo<Dog>();
        result.AsByteArray().Should().BeEquivalentTo(adv.AsByteArray());
    }

    [Fact]
    public async Task CastUserData_InterfaceMatchesImplementation_ShouldCastAndEmit()
    {
        // Arrange
        IGapAdvertisement<Dog> adv = CreateBaseAdvertisement().WithUserData(new Dog());

        // Act
        IGapAdvertisement<ICreature> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<ICreature>()
            .FirstAsync();

        // Assert
        result.UserData.Should().BeAssignableTo<Dog>();
    }

    [Fact]
    public async Task CastUserData_BaseDoesNotMatchDerived_ShouldThrowInvalidCastException()
    {
        // Arrange
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData(new Animal());

        // Act & Assert
        Func<Task> act = async () =>
            await new[] { adv }.ToObservable<IGapAdvertisement>().CastUserData<Dog>().FirstAsync();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement`1' to 'Dog'.");
    }

    [Fact]
    public async Task CastUserData_AlreadyGenericBaseWithDerivedValue_ShouldPassThroughSameInstance()
    {
        // Arrange
        IGapAdvertisement<Animal> adv = CreateBaseAdvertisement().WithUserData<Animal>(new Dog());

        // Act
        IGapAdvertisement<Animal> result = await new[] { adv }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<Animal>()
            .FirstAsync();

        // Assert
        result.Should().BeSameAs(adv);
        result.UserData.Should().BeAssignableTo<Dog>();
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
            .ToObservable()
            .CastUserData<object>()
            .ToArray();

        // Assert
        results.Should().HaveCount(3);
        results[0].UserData.Should().Be("test1");
        results[1].UserData.Should().Be(42);
        results[2].UserData.Should().Be(3.14);
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
                .ToObservable()
                .CastUserData<string>()
                .ToArray();

        await act.Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage("Cannot cast UserData on 'GapAdvertisement`1' to 'String'.");
    }

    [Fact]
    public async Task CastUserData_WithValueTypes_ShouldCastCorrectly()
    {
        // Arrange
        IGapAdvertisement<object> advWithInt = CreateBaseAdvertisement().WithUserData<object>(42);
        IGapAdvertisement<object> advWithBool = CreateBaseAdvertisement().WithUserData<object>(true);
        IGapAdvertisement<object> advWithChar = CreateBaseAdvertisement().WithUserData<object>('A');

        // Act
        var intResult = await new[] { advWithInt }.ToObservable<IGapAdvertisement>().CastUserData<int>().FirstAsync();
        var boolResult = await new[] { advWithBool }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<bool>()
            .FirstAsync();
        var charResult = await new[] { advWithChar }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<char>()
            .FirstAsync();

        // Assert
        intResult.UserData.Should().Be(42);
        boolResult.UserData.Should().Be(true);
        charResult.UserData.Should().Be('A');
    }

    [Fact]
    public async Task CastUserData_WithReferenceTypes_ShouldCastCorrectly()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        IGapAdvertisement<object> advWithObject = CreateBaseAdvertisement().WithUserData<object>(testObject);

        // Act
        var result = await new[] { advWithObject }
            .ToObservable<IGapAdvertisement>()
            .CastUserData<object>()
            .FirstAsync();

        // Assert
        result.UserData.Should().BeSameAs(testObject);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(null)]
    public async Task CastUserData_WithNullableTypes_ShouldCastCorrectly(int? data)
    {
        // Arrange
        IGapAdvertisement<object?> adv = CreateBaseAdvertisement().WithUserData<object?>(data);

        // Act
        IGapAdvertisement<int?> result1 = await new[] { adv }.ToObservable().CastUserData<int?>().FirstAsync();

        // Assert
        result1.UserData.Should().Be(data);
    }
}
