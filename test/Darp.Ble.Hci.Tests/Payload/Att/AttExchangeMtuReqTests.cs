using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttExchangeMtuReqTests
{
    private const AttOpCode ExpectedOpCode = AttOpCode.ATT_EXCHANGE_MTU_REQ;

    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttExchangeMtuReq.ExpectedOpCode.Should().Be(ExpectedOpCode);
    }

    [Theory]
    [InlineData(65, "024100")]
    public void TryEncode_ShouldBeValid(ushort clientRxMtu, string expectedHexBytes)
    {
        var buffer = new byte[3];
        var value = new AttExchangeMtuReq { ClientRxMtu = clientRxMtu };

        bool success = value.TryEncode(buffer);

        value.OpCode.Should().Be(ExpectedOpCode);
        value.Length.Should().Be(3);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryDecode_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        AttExchangeMtuReq value = default;

        bool success = value.TryEncode(buffer);
        success.Should().BeFalse();
    }
}