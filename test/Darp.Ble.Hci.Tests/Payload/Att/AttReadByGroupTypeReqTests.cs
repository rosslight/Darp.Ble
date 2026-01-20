using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByGroupTypeReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByGroupTypeReq<ushort>.ExpectedOpCode.ShouldHaveValue(0x10);
        AttReadByGroupTypeReq<Guid>.ExpectedOpCode.ShouldHaveValue(0x10);
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
            AttributeGroupType = attributeType,
        };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.ShouldBe(AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ);
        value.GetByteCount().ShouldBe(7);
        success.ShouldBeTrue();
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Theory]
    [InlineData(1, 0xFFFF, "0000FFE000001000800000805F9B34FB", "100100FFFF0000FFE000001000800000805F9B34FB")]
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
            AttributeGroupType = new Guid(Convert.FromHexString(attributeTypeHexBytes)),
        };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.ShouldBe(AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ);
        value.GetByteCount().ShouldBe(21);
        success.ShouldBeTrue();
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryReadLittleEndian_16Bit_ShouldBeInvalid()
    {
        var buffer = new byte[6];
        var value = new AttReadByGroupTypeReq<ushort>
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeGroupType = 0x2800,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }

    [Fact]
    public void TryReadLittleEndian_128Bit_ShouldBeInvalid()
    {
        var buffer = new byte[20];
        var value = new AttReadByGroupTypeReq<Guid>
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeGroupType = new Guid(Convert.FromHexString("0000FFE000001000800000805F9B34FB")),
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
