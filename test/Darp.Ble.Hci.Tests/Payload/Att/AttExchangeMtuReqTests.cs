using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttExchangeMtuReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttExchangeMtuReq.ExpectedOpCode.Should().HaveValue(0x02);
    }

    [Theory]
    [InlineData(65, "024100")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort clientRxMtu, string expectedHexBytes)
    {
        var buffer = new byte[3];
        var value = new AttExchangeMtuReq { ClientRxMtu = clientRxMtu };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_EXCHANGE_MTU_REQ);
        value.GetByteCount().Should().Be(3);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        AttExchangeMtuReq value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}