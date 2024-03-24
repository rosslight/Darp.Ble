using System.Globalization;
using Darp.Ble.Data;
using FluentAssertions;

namespace Darp.Ble.Tests.Data;

public class BleUuidTests
{

    [Theory]
    [InlineData(0x1800, "00001800-0000-1000-8000-00805F9B34FB")]
    [InlineData(0x2902, "00002902-0000-1000-8000-00805f9b34fb")]
    public void Constructor_WithUInt16_SetsTypeToUuid16(ushort value, string expectedGuid)
    {
        Guid guid = Guid.Parse(expectedGuid);
        var uuid = new BleUuid(value);

        uuid.Type.Should().Be(BleUuidType.Uuid16);
        uuid.Value.Should().Be(guid);
    }

    [Theory]
    [InlineData(0x12345678, "12345678-0000-1000-8000-00805F9B34FB")]
    [InlineData(0xAABBCCDD, "AABBCCDD-0000-1000-8000-00805F9B34FB")]
    public void Constructor_WithUInt32_SetsTypeToUuid32(uint value, string expectedGuid)
    {
        Guid guid = Guid.Parse(expectedGuid);
        var uuid = new BleUuid(value);

        uuid.Type.Should().Be(BleUuidType.Uuid32);
        uuid.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithGuid_SetsTypeToUuid128()
    {
        var guid = Guid.NewGuid();
        var uuid = new BleUuid(guid);

        uuid.Type.Should().Be(BleUuidType.Uuid128);
        uuid.Value.Should().Be(guid);
    }

    [Theory]
    [InlineData("0229", BleUuidType.Uuid16)]
    [InlineData("AABBCCDD", BleUuidType.Uuid32)]
    [InlineData("89335C0EFA27364BBEB1AD3B472AF3F1", BleUuidType.Uuid128)]
    public void Constructor_WithByteSpan_SetsCorrectType(string hexString, BleUuidType expectedType)
    {
        byte[] bytes = Convert.FromHexString(hexString);
        var uuid = new BleUuid(bytes);

        uuid.Type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("AABBCC")]
    [InlineData("AABBCCDDEE")]
    [InlineData("335C0EFA27364BBEB1AD3B472AF3F1")]
    public void Constructor_WithInvalidByteSpan_Throws(string hexString)
    {
        byte[] bytes = Convert.FromHexString(hexString);

        Action action = () =>
        {
            _ = new BleUuid(bytes);
        };

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

        var uuidString = new BleUuid(guid).ToString();

        uuidString.Should().Be(guidString);
    }

    [Fact]
    public void TryFormat_CharSpan_FormatsUuidCorrectly()
    {
        var guid = Guid.NewGuid();
        var uuid = new BleUuid(guid);
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
        var uuid = new BleUuid(guid);
        Span<byte> utf8Destination = new byte[36]; // Allocate more space than needed to ensure the GUID fits

        bool success = uuid.TryFormat(utf8Destination, out int bytesWritten, "D");

        success.Should().BeTrue();
        // Convert the written bytes back to a string for comparison
        string formattedString = System.Text.Encoding.UTF8.GetString(utf8Destination.Slice(0, bytesWritten));
        formattedString.Should().Be(guid.ToString("D"));
    }

    [Fact]
    public void TryFormat_CharSpan_FailsGracefullyWhenDestinationTooSmall()
    {
        var guid = Guid.NewGuid();
        var uuid = new BleUuid(guid);
        Span<char> destination = new char[10]; // Deliberately too small

        bool success = uuid.TryFormat(destination, out int charsWritten, "D");

        success.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void TryFormat_ByteSpan_FailsGracefullyWhenDestinationTooSmall()
    {
        var guid = Guid.NewGuid();
        var uuid = new BleUuid(guid);
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
        var bleUuid = new BleUuid(guid);

        var expected = guid.ToString(format, CultureInfo.InvariantCulture);
        var result = bleUuid.ToString(format, CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }

    [Fact]
    public void TryFormat_SpanChar_WithFormat_FormatsUuidCorrectly()
    {
        const string format = "D";
        var guid = Guid.NewGuid();
        var bleUuid = new BleUuid(guid);
        Span<char> destination = new char[36]; // Length sufficient for "D" format

        bool success = ((ISpanFormattable)bleUuid).TryFormat(destination, out int charsWritten, format, CultureInfo.InvariantCulture);

        success.Should().BeTrue();
        charsWritten.Should().Be(guid.ToString(format).Length);
        destination.ToString().Should().Be(guid.ToString(format));
    }

    [Fact]
    public void TryFormat_SpanByte_WithFormat_FormatsUuidCorrectly()
    {
        const string format = "D";
        var guid = Guid.NewGuid();
        var bleUuid = new BleUuid(guid);
        Span<byte> utf8Destination = new byte[36 * 3]; // Allocate more space than needed

        bool success = ((IUtf8SpanFormattable)bleUuid).TryFormat(utf8Destination, out int bytesWritten, format.AsSpan(), CultureInfo.InvariantCulture);

        success.Should().BeTrue();
        var expectedString = guid.ToString(format);
        string resultString = System.Text.Encoding.UTF8.GetString(utf8Destination[..bytesWritten]);

        resultString.Should().Be(expectedString);
    }

    [Fact]
    public void TryFormat_SpanChar_WhenDestinationTooSmall_ReturnsFalse()
    {
        var guid = Guid.NewGuid();
        var bleUuid = new BleUuid(guid);
        Span<char> destination = new char[10]; // Deliberately too small

        bool success = ((ISpanFormattable)bleUuid).TryFormat(destination, out int charsWritten, default, CultureInfo.InvariantCulture);

        success.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void TryFormat_SpanByte_WhenDestinationTooSmall_ReturnsFalse()
    {
        var guid = Guid.NewGuid();
        var bleUuid = new BleUuid(guid);
        Span<byte> utf8Destination = new byte[10]; // Deliberately too small

        bool success = ((IUtf8SpanFormattable)bleUuid).TryFormat(utf8Destination, out int bytesWritten, default, CultureInfo.InvariantCulture);

        success.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }
}