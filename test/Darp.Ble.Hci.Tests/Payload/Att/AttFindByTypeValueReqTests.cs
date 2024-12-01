using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttFindByTypeValueReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttFindByTypeValueReq.ExpectedOpCode.Should().HaveValue(0x06);
    }

    [Theory]
    [InlineData(1, 0xFFFF, 0x2800, "ABCD", "060100FFFF0028ABCD")]
    public void TryEncode_ShouldBeValid(ushort startingHandle,
        ushort endingHandle,
        ushort attributeType,
        string attributeValue,
        string expectedHexBytes)
    {
        var buffer = new byte[9];
        var value = new AttFindByTypeValueReq
        {
            StartingHandle = startingHandle,
            EndingHandle = endingHandle,
            AttributeType = attributeType,
            AttributeValue = Convert.FromHexString(attributeValue),
        };

        bool success = value.TryEncode(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_FIND_BY_TYPE_VALUE_REQ);
        value.Length.Should().Be(9);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Theory]
    [InlineData(6, "")]
    [InlineData(8, "ABCD")]
    [InlineData(22, "0000FFE000001000800000805F9B34FB")]
    public void TryEncode_ShouldBeInvalid(int bufferLength, string valueHexBytes)
    {
        var buffer = new byte[bufferLength];
        var value = new AttFindByTypeValueReq
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeType = 0x2800,
            AttributeValue = Convert.FromHexString(valueHexBytes),
        };

        bool success = value.TryEncode(buffer);
        success.Should().BeFalse();
    }
}