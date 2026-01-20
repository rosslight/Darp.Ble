using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByGroupTypeRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByGroupTypeRsp.ExpectedOpCode.ShouldHaveValue(0x11);
    }

    [Theory]
    [InlineData("110601000B000018", 0x0001, 0x000B, 0x1800)]
    [InlineData(
        "110601000B0000180C000F000118100022000A182300FFFF4CAA",
        0x0001,
        0x000B,
        0x1800,
        0x000C,
        0x000F,
        0x1801,
        0x0010,
        0x0022,
        0x180A,
        0x0023,
        0xFFFF,
        0xAA4C
    )]
    public void TryReadLittleEndian_16Bit_ShouldBeValid(string hexBytes, params int[] informationData)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttGroupTypeData[] attributeDataList = informationData
            .Select(x => (ushort)x)
            .PairsOf(3)
            .Select(x => new AttGroupTypeData
            {
                Handle = x[0],
                EndGroup = x[1],
                Value = BitConverter.GetBytes(x[2]),
            })
            .ToArray();

        bool success = AttReadByGroupTypeRsp.TryReadLittleEndian(
            bytes,
            out AttReadByGroupTypeRsp value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(2 + 6 * attributeDataList.Length);
        value.OpCode.ShouldBe(AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP);
        value.Length.ShouldBe<byte>(6);
        foreach (var valueTuple in value.AttributeDataList.Zip(attributeDataList))
        {
            valueTuple.First.Handle.ShouldBe(valueTuple.Second.Handle);
            valueTuple.First.EndGroup.ShouldBe(valueTuple.Second.EndGroup);
            valueTuple.First.Value.ToArray().ShouldBe(valueTuple.Second.Value.ToArray());
        }
    }

    [Theory]
    [InlineData("111401000B000000FFE000001000800000805F9B34FB", 0x0001, 0x000B, "0000FFE000001000800000805F9B34FB")]
    public void TryReadLittleEndian_128Bit_ShouldBeValid(
        string hexBytes,
        ushort startHandle,
        ushort endHandle,
        string valueHexBytes
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttGroupTypeData[] attributeDataList = [new(startHandle, endHandle, Convert.FromHexString(valueHexBytes))];

        bool success = AttReadByGroupTypeRsp.TryReadLittleEndian(
            bytes,
            out AttReadByGroupTypeRsp value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(2 + 20 * attributeDataList.Length);
        value.OpCode.ShouldBe(AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP);
        value.Length.ShouldBe<byte>(20);
        foreach (var valueTuple in value.AttributeDataList.Zip(attributeDataList))
        {
            valueTuple.First.Handle.ShouldBe(valueTuple.Second.Handle);
            valueTuple.First.EndGroup.ShouldBe(valueTuple.Second.EndGroup);
            valueTuple.First.Value.ToArray().ShouldBe(valueTuple.Second.Value.ToArray());
        }
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("100601000B000018", 0)]
    [InlineData("110601000B0000", 0)]
    [InlineData("110601000B00001800", 0)]
    [InlineData("1106", 0)]
    public void TryReadLittleEndian_16Bit_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttReadByGroupTypeRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
