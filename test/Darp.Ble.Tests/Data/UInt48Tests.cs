using System.Globalization;
using System.Runtime.InteropServices;
using Darp.Ble.Data;
using Shouldly;

namespace Darp.Ble.Tests.Data;

public sealed class UInt48Tests
{
    [Fact]
    public void MaxValue_IsCorrect()
    {
        // Arrange
        var maxValue = (UInt48)0xFFFFFFFFFFFF;

        // Assert
        UInt48.MaxValue.ShouldBe(maxValue);
    }

    [Fact]
    public void MinValue_IsCorrect()
    {
        // Arrange
        var minValue = (UInt48)0x000000000000;

        // Assert
        UInt48.MinValue.ShouldBe(minValue);
    }

    [Fact]
    public void Cast_Ulong_To_UInt48()
    {
        // Arrange
        const ulong originalValue = 0x0000FFFFFFFFFFFF; // A 48-bit max value

        // Act
        var castedValue = (UInt48)originalValue;

        // Assert
        ((ulong)castedValue).ShouldBe(originalValue);
    }

    [Fact]
    public void Cast_UInt48_To_Ulong()
    {
        // Arrange
        var originalValue = (UInt48)0x0000FFFFFFFFFFFF; // Using explicit casting to create a UInt48

        // Act
        ulong castedValue = originalValue;

        // Assert
        castedValue.ShouldBe(0x0000FFFFFFFFFFFFUL);
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
        reversed.ShouldBe(expectedValue);
    }

    [Fact]
    public void ReadLittleEndian_With_Enough_Bytes()
    {
        // Arrange
        ReadOnlySpan<byte> source = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        // Act
        UInt48 result = UInt48.ReadLittleEndian(source);

        // Assert
        result.ShouldBe(UInt48.MaxValue);
    }

    [Fact]
    public void ReadLittleEndian_Throws_On_Short_Source()
    {
        // Arrange
        byte[] source = [0xFF, 0xFF]; // Not enough bytes

        // Act
        Action act = () => UInt48.ReadLittleEndian((ReadOnlySpan<byte>)source);

        // Assert
        act.ShouldThrow<ArgumentOutOfRangeException>();
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
        destination.ShouldBe([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
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
        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0xAABBCCDDEEFF, "187723572702975")]
    public void ToString_Returns_Correctly(ulong value, string expectedString)
    {
        // Arrange
        var value1 = (UInt48)value;

        // Act
        var toString = value1.ToString();

        // Assert
        toString.ShouldBe(expectedString);
    }

    [Theory]
    [InlineData(0xAABBCCDDEEFF, 0xAABBCCDDEEFF, true)]
    public void Equals_Returns_Correctly(ulong first, ulong second, bool expectedResult)
    {
        // Arrange
        var value1 = (UInt48)first;
        var value2 = (UInt48)second;

        // Act & Assert
        value1.Equals(value2).ShouldBe(expectedResult);
        value1.Equals((object?)value2).ShouldBe(expectedResult);
        (value1 == value2).ShouldBe(expectedResult);
        (value1 != value2).ShouldBe(!expectedResult);
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
        value1.CompareTo(value2).ShouldBeNegative();
        (value1 < value2).ShouldBeTrue();
        (value1 <= value2).ShouldBeTrue();
        value2.CompareTo(value1).ShouldBePositive();
        (value1 > value2).ShouldBeFalse();
        (value1 >= value2).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0x010101010101)]
    public void CompareTo_Returns_Correct_SameValue(ulong value)
    {
        // Arrange
        var value1 = (UInt48)value;
        var value2 = (UInt48)value;

        value1.CompareTo(value2).ShouldBe(0);
        (value1 >= value2).ShouldBeTrue();
        (value1 <= value2).ShouldBeTrue();
        value1.GetHashCode().ShouldBe(value2.GetHashCode());
    }

    [Fact]
    public void ToString_With_Format_Returns_Correct_Hex()
    {
        // Arrange
        var value = (UInt48)0xAABBCCDDEEFF;
        // Act
        var formatted = value.ToString("X12", CultureInfo.InvariantCulture);
        // Assert: 12 hex digits, uppercase
        formatted.ShouldBe("AABBCCDDEEFF");
    }

    [Theory]
    [InlineData(0x001122334455, "001122334455")]
    [InlineData(0xAABBCCDDEEFF, "AABBCCDDEEFF")]
    public void TryFormat_Writes_Correct_Chars_And_Returns_True(ulong value, string expectedHexString)
    {
        // Arrange
        var u48 = (UInt48)value;
        Span<char> buffer = stackalloc char[12];
        // Act
        bool result = u48.TryFormat(buffer, out int charsWritten, "X12".AsSpan(), CultureInfo.InvariantCulture);
        // Assert
        result.ShouldBeTrue();
        charsWritten.ShouldBe(12);
        new string(buffer).ShouldBe(expectedHexString);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(0x123456789ABCUL)]
    [InlineData(0xFFFFFFFFFFFFUL)]
    public void Static_ToUInt48_And_ToUInt64_Roundtrip(ulong original)
    {
        // Act
        var u48 = UInt48.ToUInt48(original);
        var roundTripped = u48.ToUInt64();
        // Assert
        roundTripped.ShouldBe(original);
    }

    [Theory]
    [InlineData(0x000000000000UL)]
    [InlineData(0x0123456789ABUL)]
    [InlineData(0xFEDCBA987654UL)]
    public void ReadThenWrite_LittleEndian_Roundtrip(ulong value)
    {
        // Arrange
        var u48 = (UInt48)value;
        Span<byte> bytes = stackalloc byte[6];
        // Act
        UInt48.WriteLittleEndian(bytes, u48);
        UInt48 readBack = UInt48.ReadLittleEndian(bytes);
        // Assert
        readBack.ShouldBe(u48);
    }

    [Fact]
    public void MinValue_ToUInt64_Is_Zero()
    {
        // Act
        ulong zero = UInt48.MinValue.ToUInt64();
        // Assert
        zero.ShouldBe(0UL);
    }

    [Fact]
    public void StructLayoutSize_Is6Bytes()
    {
        // Act
        int size = Marshal.SizeOf<UInt48>();
        // Assert
        size.ShouldBe(6);
    }

    [Fact]
    public void HashCode_Different_For_Different_Values()
    {
        // Arrange
        var v1 = (UInt48)0x000000000001UL;
        var v2 = (UInt48)0x000000000002UL;
        // Act
        int h1 = v1.GetHashCode();
        int h2 = v2.GetHashCode();
        // Assert
        h1.ShouldNotBe(h2);
    }
}
