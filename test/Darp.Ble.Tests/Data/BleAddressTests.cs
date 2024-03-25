using Darp.Ble.Data;
using Darp.Ble.Tests.TestUtils;
using FluentAssertions;

namespace Darp.Ble.Tests.Data;

public sealed class BleAddressTests
{
    [Theory]
    [InlineData(0xAABBCCDDEEFF)]
    public void Constructor_NoAddressType_CorrectProperties(ulong rawValue)
    {
        var value = (UInt48)rawValue;
        var address = new BleAddress(value);

        address.Type.Should().Be(BleAddressType.NotAvailable);
        address.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(BleAddressType.Public, 0xAABBCCDDEEFF)]
    public void Constructor_WithAddressType_CorrectProperties(BleAddressType addressType, ulong rawValue)
    {
        var value = (UInt48)rawValue;
        var address = new BleAddress(addressType, value);

        address.Type.Should().Be(addressType);
        address.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", 0xAABBCCDDEEFF)]
    [InlineData("00:11:22:33:44:55", 0x001122334455)]
    public void Parse_ValidString_ShouldBeSuccessful(string input, ulong expectedRawValue)
    {
        var expectedValue = (UInt48)expectedRawValue;

        bool success = BleAddress.TryParse(input, provider: null, out BleAddress? result);
        BleAddress result2 = BleAddress.Parse(input, provider: null);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Type.Should().Be(BleAddressType.NotAvailable);
        result.Value.Should().Be(expectedValue);
        result2.Type.Should().Be(BleAddressType.NotAvailable);
        result2.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00:11:22:33:44")]
    [InlineData("00112233445500000")]
    [InlineData("00:11223344550000")]
    [InlineData("00:11:22334455000")]
    [InlineData("00:11:22:33445500")]
    [InlineData("00:11:22:33:44550")]
    [InlineData("GG:HH:II:JJ:KK:LL")]
    public void Parse_InvalidString_ShouldNotBeSuccessful(string? input)
    {
        // Act
        Action act = () => BleAddress.Parse(input!, provider: null);
        bool success = BleAddress.TryParse(input, provider: null, out BleAddress? result);

        // Assert
        if (input is not null)
            act.Should().Throw<FormatException>();
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Conversion_ToUInt48_ShouldWorkCorrectly()
    {
        // Arrange
        var expectedValue = new UInt48(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var bleAddress = new BleAddress(expectedValue);

        // Act
        UInt48 value = bleAddress;

        // Assert
        value.Should().Be(expectedValue);
    }

    [Fact]
    public void Conversion_FromUInt48_ShouldWorkCorrectly()
    {
        // Arrange
        var value = new UInt48(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);

        // Act
        var bleAddress = (BleAddress)value;

        // Assert
        bleAddress.Value.Should().Be(value);
        bleAddress.Type.Should().Be(BleAddressType.NotAvailable);
    }
}