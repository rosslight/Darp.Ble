using Darp.Ble.Data;
using Shouldly;

namespace Darp.Ble.Tests.Data;

public sealed class BleAddressTests
{
    [Theory]
    [InlineData(0xAABBCCDDEEFF)]
    public void Constructor_NoAddressType_CorrectProperties(ulong rawValue)
    {
        var value = (UInt48)rawValue;
        var address = new BleAddress(value);

        address.Type.ShouldBe(BleAddressType.NotAvailable);
        address.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(BleAddressType.Public, 0xAABBCCDDEEFF)]
    public void Constructor_WithAddressType_CorrectProperties(BleAddressType addressType, ulong rawValue)
    {
        var value = (UInt48)rawValue;
        var address = new BleAddress(addressType, value);

        address.Type.ShouldBe(addressType);
        address.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", 0xAABBCCDDEEFF)]
    [InlineData("00:11:22:33:44:55", 0x001122334455)]
    public void Parse_ValidString_ShouldBeSuccessful(string input, ulong expectedRawValue)
    {
        var expectedValue = (UInt48)expectedRawValue;

        bool success = BleAddress.TryParse(input, provider: null, out BleAddress? result);
        BleAddress result2 = BleAddress.Parse(input, provider: null);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result!.Type.ShouldBe(BleAddressType.NotAvailable);
        result.Value.ShouldBe(expectedValue);
        result2.Type.ShouldBe(BleAddressType.NotAvailable);
        result2.Value.ShouldBe(expectedValue);
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
            act.ShouldThrow<FormatException>();
        success.ShouldBeFalse();
        result.ShouldBeNull();
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
        value.ShouldBe(expectedValue);
    }

    [Fact]
    public void Conversion_FromUInt48_ShouldWorkCorrectly()
    {
        // Arrange
        var value = new UInt48(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);

        // Act
        var bleAddress = (BleAddress)value;

        // Assert
        bleAddress.Value.ShouldBe(value);
        bleAddress.Type.ShouldBe(BleAddressType.NotAvailable);
    }

    [Fact]
    public void ToString_Returns_12DigitUppercaseHex()
    {
        // Arrange
        var addr = new BleAddress((UInt48)0x0a1b2c3d4e5f);
        // Act
        string s = addr.ToString();
        // Assert
        s.ShouldBe("0A1B2C3D4E5F");
    }

    [Fact]
    public void TryParse_LowercaseHex_IsAccepted()
    {
        // Arrange
        string input = "aa:bb:cc:dd:ee:ff";
        // Act
        bool ok = BleAddress.TryParse(input, provider: null, out var result);
        // Assert
        ok.ShouldBeTrue();
        result!.Value.ShouldBe((UInt48)0xAABBCCDDEEFF);
    }

    [Fact]
    public void Equals_BleAddress_IgnoresNullAndTypeMismatch()
    {
        const ulong value = 0x112233445566;
        var publicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);
        var randomAddress = new BleAddress(BleAddressType.Random, (UInt48)value);
        var secondPublicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);
        BleAddress? nullAddr = null;

        publicAddress.Equals(publicAddress).ShouldBeTrue();
        publicAddress.Equals(randomAddress).ShouldBeFalse();
        publicAddress.Equals(secondPublicAddress).ShouldBeTrue();
        publicAddress.Equals(nullAddr).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0xABCDEF123456, 0x000000000000)]
    [InlineData(0x000000000000, 0xABCDEF123456)]
    public void Equals_UInt48(ulong value, ulong otherValue)
    {
        var u48 = (UInt48)value;
        var otherU48 = (UInt48)otherValue;
        var address = new BleAddress(BleAddressType.Public, u48);

        address.Equals(u48).ShouldBeTrue();
        address.Equals(value).ShouldBeTrue();
        address.Equals(otherU48).ShouldBeFalse();
        address.Equals(otherValue).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0x010203040506)]
    [InlineData(0x000000000000)]
    [InlineData(0xFFFFFFFFFFFF)]
    public void GetHashCode_MatchesEqualityLogic(ulong value)
    {
        var randomAddress = new BleAddress(BleAddressType.Random, (UInt48)value);
        var otherRandomAddress = new BleAddress(BleAddressType.Random, (UInt48)value);
        var publicAddress = new BleAddress(BleAddressType.Public, (UInt48)value);

        randomAddress.GetHashCode().ShouldBe(otherRandomAddress.GetHashCode());
        randomAddress.GetHashCode().ShouldNotBe(publicAddress.GetHashCode());
    }

    [Fact]
    public void NotAvailable_StaticProperty_IsZeroAndNotAvailableType()
    {
        BleAddress notAvailableAddress = BleAddress.NotAvailable;
        notAvailableAddress.Type.ShouldBe(BleAddressType.NotAvailable);
        notAvailableAddress.Value.ShouldBe(UInt48.Zero);
    }

    [Fact]
    public void NewRandomStaticAddress_HasHighTwoBitsSetAndStaticRandomKind()
    {
        var rand = BleAddress.NewRandomStaticAddress();

        // the top two bits of the 48‐bit value must be 11
        ulong hi2 = (rand.Value >> 46) & 0b11;
        hi2.ShouldBe(0b11UL);
        rand.Type.ShouldBe(BleAddressType.Random);
        rand.RandomKind.ShouldBe(BleRandomAddressKind.Static);
    }

    [Theory]
    [InlineData(0b00, BleRandomAddressKind.NonResolvablePrivate)]
    [InlineData(0b01, BleRandomAddressKind.ResolvablePrivate)]
    [InlineData(0b11, BleRandomAddressKind.Static)]
    [InlineData(0b10, null)]
    public void CreateRandomAddress_InfersRandomKindFromTopTwoBits(int bits, BleRandomAddressKind? expectedKind)
    {
        // put bits into bits 47-46
        var u48 = (UInt48)(((ulong)bits << 46) | 0x1122_3344_5566UL);

        var addr = BleAddress.CreateRandomAddress(u48);

        addr.Value.ShouldBe(u48);
        addr.Type.ShouldBe(BleAddressType.Random);
        addr.RandomKind.ShouldBe(expectedKind);
    }

    [Theory]
    [InlineData(BleAddressType.Public)]
    [InlineData(BleAddressType.PublicIdentity)]
    [InlineData(BleAddressType.RandomStaticIdentity)]
    [InlineData(BleAddressType.NotAvailable)]
    public void RandomKind_AddressTypeIsNotRandom_IsNull(BleAddressType addressType)
    {
        var address = new BleAddress(addressType, (UInt48)0xC11122334455);

        address.RandomKind.ShouldBeNull();
    }
}
