using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByGroupTypeReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByGroupTypeReq<ushort>.ExpectedOpCode.Should().HaveValue(0x10);
        AttReadByGroupTypeReq<Guid>.ExpectedOpCode.Should().HaveValue(0x10);
    }

    [Theory]
    [InlineData(1, 0xFFFF, 0x2800, "100100FFFF0028")]
    public void TryWriteLittleEndian_16Bit_ShouldBeValid(
        ushort startingHandle,
        ushort endingHandle,
        ushort attributeType,
        string expectedHexBytes
    )
    {
        var buffer = new byte[7];
        var value = new AttReadByGroupTypeReq<ushort>
        {
            StartingHandle = startingHandle,
            EndingHandle = endingHandle,
            AttributeType = attributeType,
        };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ);
        value.GetByteCount().Should().Be(7);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Theory]
    [InlineData(
        1,
        0xFFFF,
        "0000FFE000001000800000805F9B34FB",
        "100100FFFF0000FFE000001000800000805F9B34FB"
    )]
    public void TryWriteLittleEndian_128Bit_ShouldBeValid(
        ushort startingHandle,
        ushort endingHandle,
        string attributeTypeHexBytes,
        string expectedHexBytes
    )
    {
        var buffer = new byte[21];
        var value = new AttReadByGroupTypeReq<Guid>
        {
            StartingHandle = startingHandle,
            EndingHandle = endingHandle,
            AttributeType = new Guid(Convert.FromHexString(attributeTypeHexBytes)),
        };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ);
        value.GetByteCount().Should().Be(21);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryReadLittleEndian_16Bit_ShouldBeInvalid()
    {
        var buffer = new byte[6];
        var value = new AttReadByGroupTypeReq<ushort>
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeType = 0x2800,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryReadLittleEndian_128Bit_ShouldBeInvalid()
    {
        var buffer = new byte[20];
        var value = new AttReadByGroupTypeReq<Guid>
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeType = new Guid(Convert.FromHexString("0000FFE000001000800000805F9B34FB")),
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
