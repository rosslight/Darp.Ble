using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttExchangeMtuRspTests
{
    private const AttOpCode ExpectedOpCode = AttOpCode.ATT_EXCHANGE_MTU_RSP;

    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttExchangeMtuRsp.ExpectedOpCode.Should().Be(ExpectedOpCode);
    }

    [Theory]
    [InlineData("034100", 65)]
    [InlineData("03410000", 65)]
    public void TryDecode_ShouldBeValid(string hexBytes, ushort serverRxMtu)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttExchangeMtuRsp.TryDecode(bytes, out AttExchangeMtuRsp value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(3);
        value.OpCode.Should().Be(ExpectedOpCode);
        value.ServerRxMtu.Should().Be(serverRxMtu);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0341", 0)]
    [InlineData("024100", 0)]
    public void TryDecode_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttExchangeMtuRsp.TryDecode(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}