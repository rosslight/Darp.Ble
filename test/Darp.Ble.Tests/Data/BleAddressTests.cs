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

    [Fact]
    public void ToString_Returns_12DigitUppercaseHex()
    {
        // Arrange
        var addr = new BleAddress((UInt48)0x0a1b2c3d4e5f);
        // Act
        string s = addr.ToString();
        // Assert
        s.Should().Be("0A1B2C3D4E5F");
    }

    [Fact]
    public void TryParse_LowercaseHex_IsAccepted()
    {
        // Arrange
        string input = "aa:bb:cc:dd:ee:ff";
        // Act
        bool ok = BleAddress.TryParse(input, provider: null, out var result);
        // Assert
        ok.Should().BeTrue();
        result!.Value.Should().Be((UInt48)0xAABBCCDDEEFF);
    }

    [Fact]
    public void Equals_BleAddress_IgnoresNullAndTypeMismatch()
    {
        const ulong value = 0x112233445566;
        var publicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);
        var randomAddress = new BleAddress(BleAddressType.RandomStatic, (UInt48)value);
        var secondPublicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);
        BleAddress? nullAddr = null;

        publicAddress.Equals(publicAddress).Should().BeTrue();
        publicAddress.Equals(randomAddress).Should().BeFalse();
        publicAddress.Equals(secondPublicAddress).Should().BeTrue();
        publicAddress.Equals(nullAddr).Should().BeFalse();
    }

    [Theory]
    [InlineData(0xABCDEF123456, 0x000000000000)]
    [InlineData(0x000000000000, 0xABCDEF123456)]
    public void Equals_UInt48(ulong value, ulong otherValue)
    {
        var u48 = (UInt48)value;
        var otherU48 = (UInt48)otherValue;
        var address = new BleAddress(BleAddressType.Public, u48);

        address.Equals(u48).Should().BeTrue();
        address.Equals(value).Should().BeTrue();
        address.Equals(otherU48).Should().BeFalse();
        address.Equals(otherValue).Should().BeFalse();
    }

    [Theory]
    [InlineData(0x010203040506)]
    [InlineData(0x000000000000)]
    [InlineData(0xFFFFFFFFFFFF)]
    public void GetHashCode_MatchesEqualityLogic(ulong value)
    {
        var randomAddress = new BleAddress(BleAddressType.RandomStatic, (UInt48)value);
        var otherRandomAddress = new BleAddress(BleAddressType.RandomStatic, (UInt48)value);
        var publicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);

        randomAddress.GetHashCode().Should().Be(otherRandomAddress.GetHashCode());
        randomAddress.GetHashCode().Should().NotBe(publicAddress.GetHashCode());
    }

    [Fact]
    public void NotAvailable_StaticProperty_IsZeroAndNotAvailableType()
    {
        BleAddress notAvailableAddress = BleAddress.NotAvailable;
        notAvailableAddress.Type.Should().Be(BleAddressType.NotAvailable);
        notAvailableAddress.Value.Should().Be(UInt48.Zero);
    }

    [Fact]
    public void NewRandomStaticAddress_HasHighTwoBitsSetAndTypeRandomStatic()
    {
        var rand = BleAddress.NewRandomStaticAddress();

        // the top two bits of the 48‐bit value must be 11
        ulong hi2 = (rand.Value >> 46) & 0b11;
        hi2.Should().Be(0b11);
        rand.Type.Should().Be(BleAddressType.RandomStatic);
    }

    [Theory]
    [InlineData(0b00, BleAddressType.RandomPrivateNonResolvable)]
    [InlineData(0b01, BleAddressType.RandomPrivateResolvable)]
    [InlineData(0b11, BleAddressType.RandomStatic)]
    [InlineData(0b10, BleAddressType.NotAvailable)]
    public void CreateRandomAddress_InfersTypeFromTopTwoBits(int bits, BleAddressType expectedType)
    {
        // put bits into bits 47-46
        ulong raw = ((ulong)bits << 46) | 0x0000_0000_0000UL;
        var u48 = (UInt48)raw;

        var addr = BleAddress.CreateRandomAddress(u48);
        addr.Value.Should().Be(u48);
        addr.Type.Should().Be(expectedType);
    }
}
