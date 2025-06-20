using System.Globalization;
using System.Text;
using Darp.Ble.Data;
using FluentAssertions;

namespace Darp.Ble.Tests.Data;

public sealed class BleUuidTests
{
    [Fact]
    public void Construct_BleUuid_InvalidType_ShouldThrow()
    {
        // Act
        Func<BleUuid> act = () => new BleUuid((BleUuidType)9999, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0x1800, "00001800-0000-1000-8000-00805F9B34FB")]
    [InlineData(0x2902, "00002902-0000-1000-8000-00805f9b34fb")]
    public void Constructor_WithUInt16_SetsTypeToUuid16(ushort value, string expectedGuid)
    {
        Guid guid = Guid.Parse(expectedGuid);
        BleUuid uuid = BleUuid.FromUInt16(value);

        uuid.Type.Should().Be(BleUuidType.Uuid16);
        uuid.Value.Should().Be(guid);
    }

    [Theory]
    [InlineData(0x12345678, "12345678-0000-1000-8000-00805F9B34FB")]
    [InlineData(0xAABBCCDD, "AABBCCDD-0000-1000-8000-00805F9B34FB")]
    public void Constructor_WithUInt32_SetsTypeToUuid32(uint value, string expectedGuid)
    {
        Guid guid = Guid.Parse(expectedGuid);
        BleUuid uuid = BleUuid.FromUInt32(value);

        uuid.Type.Should().Be(BleUuidType.Uuid32);
        uuid.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithGuid_SetsTypeToUuid128()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);

        uuid.Type.Should().Be(BleUuidType.Uuid128);
        uuid.Value.Should().Be(guid);
    }

    [Theory]
    [InlineData(0xabcd, "cdab")]
    public void Constructor_WithByteSpanAndUShort_IsEquivalent(ushort value, string hexString)
    {
        byte[] bytes = Convert.FromHexString(hexString);
        BleUuid bytesUuid = BleUuid.Read(bytes);
        BleUuid uint16Uuid = value;

        bytesUuid.Should().Be(uint16Uuid);
    }

    [Theory]
    [InlineData("0229", BleUuidType.Uuid16)]
    [InlineData("AABBCCDD", BleUuidType.Uuid32)]
    [InlineData("89335C0EFA27364BBEB1AD3B472AF3F1", BleUuidType.Uuid128)]
    public void Constructor_WithByteSpan_SetsCorrectType(string hexString, BleUuidType expectedType)
    {
        byte[] bytes = Convert.FromHexString(hexString);
        BleUuid bytesUuid = BleUuid.Read(bytes);

        bytesUuid.Type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("AABBCC")]
    [InlineData("AABBCCDDEE")]
    [InlineData("335C0EFA27364BBEB1AD3B472AF3F1")]
    public void Constructor_WithInvalidByteSpan_Throws(string hexString)
    {
        byte[] bytes = Convert.FromHexString(hexString);

        Action action = () => _ = BleUuid.Read(bytes);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb")]
    public void Parse_ReturnsCorrectTypeAndValue(string guidString)
    {
        Guid expectedGuid = Guid.Parse(guidString);

        BleUuid uuid = BleUuid.Parse(guidString, null);

        uuid.Type.Should().Be(BleUuidType.Uuid128);
        uuid.Value.Should().Be(expectedGuid);
    }

    [Theory]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb")]
    public void TryParse_ParsesValidString(string guidString)
    {
        Guid expectedGuid = Guid.Parse(guidString);

        bool success = BleUuid.TryParse(guidString, null, out BleUuid? uuid);

        success.Should().BeTrue();
        uuid.Should().NotBeNull();
        uuid!.Type.Should().Be(BleUuidType.Uuid128);
        uuid.Value.Should().Be(expectedGuid);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData(null)]
    public void TryParse_ReturnsFalseForInvalidString(string? invalidGuidString)
    {
        bool success = BleUuid.TryParse(invalidGuidString, null, out BleUuid? uuid);

        success.Should().BeFalse();
        uuid.Should().BeNull();
    }

    [Theory]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb")]
    public void ToString_FormatsUuidCorrectly(string guidString)
    {
        Guid guid = Guid.Parse(guidString);

        var uuidString = BleUuid.FromGuid(guid).ToString();

        uuidString.Should().Be(guidString);
    }

    [Fact]
    public void TryFormat_CharSpan_FormatsUuidCorrectly()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<char> destination = new char[36]; // GUID string length without hyphens

        bool success = uuid.TryFormat(destination, out int charsWritten, "D");

        success.Should().BeTrue();
        charsWritten.Should().Be(36);
        destination.ToString().Should().Be(guid.ToString("D"));
    }

    [Fact]
    public void TryFormat_ByteSpan_FormatsUuidCorrectly()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<byte> utf8Destination = new byte[36]; // Allocate more space than needed to ensure the GUID fits

        bool success = uuid.TryFormat(utf8Destination, out int bytesWritten, "D");

        success.Should().BeTrue();
        // Convert the written bytes back to a string for comparison
        string formattedString = Encoding.UTF8.GetString(utf8Destination.Slice(0, bytesWritten));
        formattedString.Should().Be(guid.ToString("D"));
    }

    [Fact]
    public void TryFormat_CharSpan_FailsGracefullyWhenDestinationTooSmall()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<char> destination = new char[10]; // Deliberately too small

        bool success = uuid.TryFormat(destination, out int charsWritten, "D");

        success.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void TryFormat_ByteSpan_FailsGracefullyWhenDestinationTooSmall()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<byte> utf8Destination = new byte[10]; // Deliberately too small

        bool success = uuid.TryFormat(utf8Destination, out int bytesWritten, "D");

        success.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ToString_WithFormat_FormatsUuidCorrectly()
    {
        const string format = "N";
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);

        var expected = guid.ToString(format, CultureInfo.InvariantCulture);
        var result = uuid.ToString(format, CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }

    [Fact]
    public void TryFormat_SpanChar_WithFormat_FormatsUuidCorrectly()
    {
        const string format = "D";
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<char> destination = new char[36]; // Length sufficient for "D" format

        bool success = ((ISpanFormattable)uuid).TryFormat(
            destination,
            out int charsWritten,
            format,
            CultureInfo.InvariantCulture
        );

        success.Should().BeTrue();
        charsWritten.Should().Be(guid.ToString(format).Length);
        destination.ToString().Should().Be(guid.ToString(format));
    }

    [Fact]
    public void TryFormat_SpanByte_WithFormat_FormatsUuidCorrectly()
    {
        const string format = "D";
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<byte> utf8Destination = new byte[36 * 3]; // Allocate more space than needed

        bool success = ((IUtf8SpanFormattable)uuid).TryFormat(
            utf8Destination,
            out int bytesWritten,
            format.AsSpan(),
            CultureInfo.InvariantCulture
        );

        success.Should().BeTrue();
        var expectedString = guid.ToString(format);
        string resultString = System.Text.Encoding.UTF8.GetString(utf8Destination[..bytesWritten]);

        resultString.Should().Be(expectedString);
    }

    [Fact]
    public void TryFormat_SpanChar_WhenDestinationTooSmall_ReturnsFalse()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<char> destination = new char[10]; // Deliberately too small

        bool success = ((ISpanFormattable)uuid).TryFormat(
            destination,
            out int charsWritten,
            default,
            CultureInfo.InvariantCulture
        );

        success.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void TryFormat_SpanByte_WhenDestinationTooSmall_ReturnsFalse()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid);
        Span<byte> utf8Destination = new byte[10]; // Deliberately too small

        bool success = ((IUtf8SpanFormattable)uuid).TryFormat(
            utf8Destination,
            out int bytesWritten,
            default,
            CultureInfo.InvariantCulture
        );

        success.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void TryWriteBytes_WithUuid16AndSufficientDestination_ReturnsTrueAndCopiesExpectedBytes()
    {
        BleUuid bleUuid = 0xAABB;

        Span<byte> destination = new byte[2];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeTrue();
        destination.ToArray().Should().BeEquivalentTo([0xBB, 0xAA]);
    }

    [Fact]
    public void TryWriteBytes_WithUuid16AndInsufficientDestination_ReturnsFalseAndDestinationRemainsUnchanged()
    {
        BleUuid bleUuid = 0xAABB;

        Span<byte> destination = [0xFF];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeFalse();
        // It hasn't been overwritten
        destination[0].Should().Be(0xFF);
    }

    [Fact]
    public void TryWriteBytes_WithUuid32AndSufficientDestination_ReturnsTrueAndCopiesExpectedBytes()
    {
        BleUuid bleUuid = BleUuid.FromUInt32(0xAABBCCDD);

        Span<byte> destination = new byte[4];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeTrue();
        destination.ToArray().Should().BeEquivalentTo([0xDD, 0xCC, 0xBB, 0xAA]);
    }

    [Fact]
    public void TryWriteBytes_WithUuid32AndInsufficientDestination_ReturnsFalseAndDestinationRemainsUnchanged()
    {
        BleUuid bleUuid = BleUuid.FromUInt32(0xAABBCCDD);

        Span<byte> destination = [0xFF, 0xFF, 0xFF];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeFalse();
        destination.ToArray().Should().BeEquivalentTo([0xFF, 0xFF, 0xFF]);
    }

    [Fact]
    public void TryWriteBytes_WithUuid128AndSufficientDestination_ReturnsTrueAndCopiesAll16Bytes()
    {
        // Arrange
        var testGuid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
        // [ 04, 03, 02, 01, 06, 05, 08, 07, 09, 0A, 0B, 0C, 0D, 0E, 0F, 10 ]
        BleUuid bleUuid = BleUuid.FromGuid(testGuid);

        Span<byte> destination = new byte[16];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeTrue();
        destination.Length.Should().Be(16);
        destination
            .ToArray()
            .Should()
            .BeEquivalentTo(
                [0x04, 0x03, 0x02, 0x01, 0x06, 0x05, 0x08, 0x07, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10]
            );
    }

    [Fact]
    public void TryWriteBytes_WithUuid128AndInsufficientDestination_ReturnsFalse()
    {
        // Arrange
        var testGuid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
        BleUuid bleUuid = BleUuid.FromGuid(testGuid);

        Span<byte> destination = new byte[15];

        // Act
        bool result = bleUuid.TryWriteBytes(destination);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("00001800-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid16)]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb", BleUuidType.Uuid16)]
    [InlineData("12345678-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid32)]
    [InlineData("AABBCCDD-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid32)]
    [InlineData("00000000-0000-0000-0000-000000000000", BleUuidType.Uuid128)]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid128)]
    public void FromGuid_WithInferTrue_DetectsExpectedType(string guidString, BleUuidType expectedType)
    {
        Guid guid = Guid.Parse(guidString);

        BleUuid bleUuid = BleUuid.FromGuid(guid, inferType: true);

        bleUuid.Type.Should().Be(expectedType);
    }

    [Fact]
    public void ToString_EmitsShortFormsFor16()
    {
        var uuid = (BleUuid)0x00FF;
        uuid.Type.Should().Be(BleUuidType.Uuid16);
        uuid.ToString().Should().Be("00FF");
    }

    [Fact]
    public void ToString_EmitsShortFormsFor32()
    {
        var uuid = BleUuid.FromUInt32(0xDEADBEEF);
        uuid.Type.Should().Be(BleUuidType.Uuid32);
        uuid.ToString().Should().Be("DEADBEEF");
    }

    [Fact]
    public void ToString_WithFormat_PassesThroughFor128()
    {
        var guid = Guid.NewGuid();
        BleUuid uuid = BleUuid.FromGuid(guid, inferType: false);
        var fmt = uuid.ToString("D", CultureInfo.InvariantCulture);
        fmt.Should().Be(guid.ToString("D", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("00001800-0000-1000-8000-00805F9B34FB", "1800")]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb", "2902")]
    [InlineData("12345678-0000-1000-8000-00805F9B34FB", "12345678")]
    [InlineData("AABBCCDD-0000-1000-8000-00805F9B34FB", "AABBCCDD")]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", "00001800-0000-0000-8000-00805f9b34fb")]
    public void TryFormat_CharSpan(string guidString, string expectedFormatString)
    {
        BleUuid uuid = BleUuid.FromGuid(Guid.Parse(guidString), inferType: true);
        Span<char> destination = stackalloc char[36];

        uuid.TryFormat(destination, out int bytesWritten).Should().BeTrue();

        bytesWritten.Should().Be(expectedFormatString.Length);
        new string(destination[..bytesWritten]).Should().Be(expectedFormatString);
    }

    [Theory]
    [InlineData("00001800-0000-1000-8000-00805F9B34FB", "1800")]
    [InlineData("00002902-0000-1000-8000-00805f9b34fb", "2902")]
    [InlineData("12345678-0000-1000-8000-00805F9B34FB", "12345678")]
    [InlineData("AABBCCDD-0000-1000-8000-00805F9B34FB", "AABBCCDD")]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", "00001800-0000-0000-8000-00805f9b34fb")]
    public void TryFormat_ByteSpan(string guidString, string expectedFormatString)
    {
        BleUuid uuid = BleUuid.FromGuid(Guid.Parse(guidString), inferType: true);
        Span<byte> destination = stackalloc byte[36];

        uuid.TryFormat(destination, out int bytesWritten, "D").Should().BeTrue();

        bytesWritten.Should().Be(expectedFormatString.Length);
        string formattedUuid = Encoding.UTF8.GetString(destination[..bytesWritten]);
        formattedUuid.Should().Be(expectedFormatString);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(15)]
    public void TryRead_InvalidLengths_ReturnsFalse(int len)
    {
        var span = new byte[len];
        BleUuid.TryRead(span, out BleUuid? result).Should().BeFalse();
        result.Should().BeNull();
        Func<BleUuid> act = () => BleUuid.Read(span);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("0018", "00001800-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid16)]
    [InlineData("0229", "00002902-0000-1000-8000-00805f9b34fb", BleUuidType.Uuid16)]
    [InlineData("78563412", "12345678-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid32)]
    [InlineData("DDCCBBAA", "AABBCCDD-0000-1000-8000-00805F9B34FB", BleUuidType.Uuid32)]
    [InlineData("00000000000000000000000000000000", "00000000-0000-0000-0000-000000000000", BleUuidType.Uuid128)]
    [InlineData("0018000000000000800000805f9b34fb", "00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid128)]
    [InlineData("0018000000000000800000805f9b34fb00", "00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid128)]
    public void TryRead_ValidLengths_ReturnsTrueAndCorrectType(
        string hexString,
        string expectedGuidString,
        BleUuidType expectedType
    )
    {
        byte[] uuidBytes = Convert.FromHexString(hexString);

        BleUuid.TryRead(uuidBytes, out BleUuid? bleUuid).Should().BeTrue();

        bleUuid.Should().NotBeNull();
        bleUuid!.Type.Should().Be(expectedType);
        bleUuid.Value.Should().Be(Guid.Parse(expectedGuidString));
    }

    [Theory]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid16, "0018")]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid32, "00180000")]
    [InlineData("00001800-0000-0000-8000-00805F9B34FB", BleUuidType.Uuid128, "0018000000000000800000805f9b34fb")]
    public void ToByteArray_ShouldWork(string guidString, BleUuidType uuidType, string expectedBytesString)
    {
        byte[] expectedBytes = Convert.FromHexString(expectedBytesString);
        var uuid = new BleUuid(uuidType, Guid.Parse(guidString));

        byte[] bytes = uuid.ToByteArray();

        bytes.Should().BeEquivalentTo(expectedBytes);
    }

    [Theory]
    [InlineData("00001800-0000-1000-8000-00805f9b34fb", 0x1800)]
    public void Equals_GuidAndNumericOverloads_Uuid16_WorkAsDocumented(string baseGuidString, ushort shortUuid)
    {
        Guid baseGuid = Guid.Parse(baseGuidString);
        BleUuid uuid = BleUuid.FromGuid(baseGuid, inferType: true);
        uuid.Type.Should().Be(BleUuidType.Uuid16);
        uuid.Equals(baseGuid).Should().BeTrue();
        uuid.Equals((uint)shortUuid).Should().BeFalse(); // 16-bit only matches ushort
        uuid.Equals(shortUuid).Should().BeTrue();
    }

    [Theory]
    [InlineData("ABCDEF01-0000-1000-8000-00805f9b34fb", 0xABCD, 0xABCDEF01)]
    public void Equals_GuidAndNumericOverloads_Uuid32_WorkAsDocumented(
        string baseGuidString,
        ushort shortUuid,
        uint intUuid
    )
    {
        Guid baseGuid = Guid.Parse(baseGuidString);
        BleUuid uuid = BleUuid.FromGuid(baseGuid, inferType: true);

        uuid.Type.Should().Be(BleUuidType.Uuid32);
        uuid.Equals(baseGuid).Should().BeTrue();
        uuid.Equals(shortUuid).Should().BeFalse();
        uuid.Equals(intUuid).Should().BeTrue();
    }
}
