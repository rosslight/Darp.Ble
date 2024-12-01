using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByGroupTypeRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByGroupTypeRsp<ushort>.ExpectedOpCode.Should().HaveValue(0x11);
        AttReadByGroupTypeRsp<Guid>.ExpectedOpCode.Should().HaveValue(0x11);
    }

    [Theory]
    [InlineData("110601000B000018", 0x0001, 0x000B, 0x1800)]
    [InlineData("110601000B0000180C000F000118100022000A182300FFFF4CAA",
        0x0001, 0x000B, 0x1800,
        0x000C, 0x000F, 0x1801,
        0x0010, 0x0022, 0x180A,
        0x0023, 0xFFFF, 0xAA4C)]
    public void TryDecode_16Bit_ShouldBeValid(string hexBytes,
        params int[] informationData)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttGroupTypeData<ushort>[] attributeDataList = informationData
            .Select(x => (ushort)x)
            .PairsOf(3)
            .Select(x => new AttGroupTypeData<ushort>()
            {
                Handle = x[0],
                EndGroup = x[1],
                Value = x[2],
            })
            .ToArray();

        bool success = AttReadByGroupTypeRsp<ushort>.TryDecode(bytes, out AttReadByGroupTypeRsp<ushort> value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(2 + 6 * attributeDataList.Length);
        value.OpCode.Should().Be(AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP);
        value.Length.Should().Be(6);
        value.AttributeDataList.Should().BeEquivalentTo(attributeDataList);
    }

    [Theory]
    [InlineData("111401000B000000FFE000001000800000805F9B34FB", 0x0001, 0x000B, "0000FFE000001000800000805F9B34FB")]
    public void TryDecode_128Bit_ShouldBeValid(string hexBytes,
        ushort startHandle, ushort endHandle, string valueHexBytes)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttGroupTypeData<Guid>[] attributeDataList =
        [
            new(startHandle, endHandle, new Guid(Convert.FromHexString(valueHexBytes))),
        ];

        bool success = AttReadByGroupTypeRsp<Guid>.TryDecode(bytes, out AttReadByGroupTypeRsp<Guid> value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(2 + 20 * attributeDataList.Length);
        value.OpCode.Should().Be(AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP);
        value.Length.Should().Be(20);
        value.AttributeDataList.Should().BeEquivalentTo(attributeDataList);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("100601000B000018", 0)]
    [InlineData("110601000B0000", 0)]
    [InlineData("110601000B00001800", 0)]
    [InlineData("1106", 0)]
    [InlineData("111401000B000000FFE000001000800000805F9B34FB", 0)]
    public void TryDecode_16Bit_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttReadByGroupTypeRsp<ushort>.TryDecode(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}