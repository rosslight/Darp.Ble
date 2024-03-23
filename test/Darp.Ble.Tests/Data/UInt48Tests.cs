using Darp.Ble.Data;
using FluentAssertions;

namespace Darp.Ble.Tests.Data;

public sealed class UInt48Tests
{
    [Fact]
    public void MaxValue_IsCorrect()
    {
        // Arrange
        var maxValue = (UInt48)0xFFFFFFFFFFFF;

        // Assert
        UInt48.MaxValue.Should().Be(maxValue);
    }

    [Fact]
    public void MinValue_IsCorrect()
    {
        // Arrange
        var minValue = (UInt48)0x000000000000;

        // Assert
        UInt48.MinValue.Should().Be(minValue);
    }

    [Fact]
    public void Cast_Ulong_To_UInt48()
    {
        // Arrange
        const ulong originalValue = 0x0000FFFFFFFFFFFF; // A 48-bit max value

        // Act
        var castedValue = (UInt48)originalValue;

        // Assert
        ((ulong)castedValue).Should().Be(originalValue);
    }

    [Fact]
    public void Cast_UInt48_To_Ulong()
    {
        // Arrange
        var originalValue = (UInt48)0x0000FFFFFFFFFFFF; // Using explicit casting to create a UInt48

        // Act
        ulong castedValue = originalValue;

        // Assert
        castedValue.Should().Be(0x0000FFFFFFFFFFFF);
    }

    [Fact]
    public void ReverseEndianness()
    {
        // Arrange
        var testValue = (UInt48)0xAABBCCDDEEFF;
        var expectedValue = (UInt48)0xFFEEDDCCBBAA;

        // Act
        UInt48 reversed = UInt48.ReverseEndianness(testValue);

        // Assert
        reversed.Should().Be(expectedValue);
    }

    [Fact]
    public void ReadLittleEndian_With_Enough_Bytes()
    {
        // Arrange
        ReadOnlySpan<byte> source = stackalloc byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

        // Act
        UInt48 result = UInt48.ReadLittleEndian(source);

        // Assert
        result.Should().Be(UInt48.MaxValue);
    }

    [Fact]
    public void ReadLittleEndian_Throws_On_Short_Source()
    {
        // Arrange
        byte[] source = [0xFF, 0xFF]; // Not enough bytes

        // Act
        Action act = () => UInt48.ReadLittleEndian((ReadOnlySpan<byte>)source);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WriteLittleEndian_Writes_Correct_Bytes()
    {
        // Arrange
        var destination = new byte[6];
        var value = UInt48.MaxValue;

        // Act
        UInt48.WriteLittleEndian((Span<byte>)destination, value);

        // Assert
        destination.Should().Equal([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
    }

    [Fact]
    public void WriteLittleEndian_Throws_On_Short_Destination()
    {
        // Arrange
        var destination = new byte[4]; // Not long enough
        var value = UInt48.MaxValue;

        // Act
        Action act = () => UInt48.WriteLittleEndian((Span<byte>)destination, value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0x010000000000, 0x020000000000)]
    [InlineData(0x010100000000, 0x010200000000)]
    [InlineData(0x010101000000, 0x010102000000)]
    [InlineData(0x010101010000, 0x010101020000)]
    [InlineData(0x010101010100, 0x010101010200)]
    [InlineData(0x010101010101, 0x010101010102)]
    public void CompareTo_Returns_Correct_Ordering(ulong first, ulong second)
    {
        // Arrange
        var value1 = (UInt48)first;
        var value2 = (UInt48)second;

        // Act & Assert
        value1.CompareTo(value2).Should().BeNegative();
        value2.CompareTo(value1).Should().BePositive();
        value1.CompareTo(value1).Should().Be(0);
    }
}