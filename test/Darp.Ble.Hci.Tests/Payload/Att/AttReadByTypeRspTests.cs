using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttReadByTypeRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttReadByTypeRsp.ExpectedOpCode.Should().HaveValue(0x09);
    }

    [Theory]
    [InlineData("0907180008190061FF", 0x0018, "08190061FF")]
    [InlineData("0907180008190061FF1B00101C0062FF",
        0x0018, "08190061FF",
        0x001B, "101C0062FF")]
    public void TryDecode_ShouldBeValid(string hexBytes,
        params object[] typeData)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttReadByTypeData[] dataList = typeData
            .Pairs()
            .Select(x => new AttReadByTypeData()
            {
                Handle = (ushort)(int)x.First,
                Value = Convert.FromHexString((string)x.Second),
            })
            .ToArray();

        bool success = AttReadByTypeRsp.TryDecode(bytes, out AttReadByTypeRsp value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(2 + 7 * dataList.Length);
        value.OpCode.Should().Be(AttOpCode.ATT_READ_BY_TYPE_RSP);
        value.Length.Should().Be(7);
        value.AttributeDataList.Zip(dataList).Should().AllSatisfy(x =>
        {
            x.First.Handle.Should().Be(x.Second.Handle);
            x.First.Value.ToArray().Should().BeEquivalentTo(x.Second.Value.ToArray());
        });
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0807180008190061FF", 0)]
    [InlineData("0907180008190061", 0)]
    [InlineData("0907180008190061FF00", 0)]
    [InlineData("0907", 0)]
    [InlineData("0901180008190061FF", 0)]
    public void TryDecode_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttReadByTypeRsp.TryDecode(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}