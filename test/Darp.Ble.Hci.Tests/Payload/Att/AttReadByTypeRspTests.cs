using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByTypeRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByTypeRsp.ExpectedOpCode.ShouldHaveValue(0x09);
    }

    [Theory]
    [InlineData("0907180008190061FF", 0x0018, "08190061FF")]
    [InlineData("0907180008190061FF1B00101C0062FF", 0x0018, "08190061FF", 0x001B, "101C0062FF")]
    public void TryReadLittleEndian_ShouldBeValid(string hexBytes, params object[] typeData)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttReadByTypeData[] dataList = typeData
            .Pairs()
            .Select(x => new AttReadByTypeData
            {
                Handle = (ushort)(int)x.First,
                Value = Convert.FromHexString((string)x.Second),
            })
            .ToArray();

        bool success = AttReadByTypeRsp.TryReadLittleEndian(bytes, out AttReadByTypeRsp value, out int decoded);

        success.ShouldBeTrue();
        decoded.ShouldBe(2 + 7 * dataList.Length);
        value.OpCode.ShouldBe(AttOpCode.ATT_READ_BY_TYPE_RSP);
        value.Length.ShouldBe<byte>(7);
        value
            .AttributeDataList.Zip(dataList)
            .ShouldAllSatisfy(x =>
            {
                x.First.Handle.ShouldBe(x.Second.Handle);
                x.First.Value.ToArray().ShouldBe(x.Second.Value.ToArray());
            });
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0807180008190061FF", 0)]
    [InlineData("0907180008190061", 0)]
    [InlineData("0907180008190061FF00", 0)]
    [InlineData("0907", 0)]
    [InlineData("0901180008190061FF", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttReadByTypeRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
