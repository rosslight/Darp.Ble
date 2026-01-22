using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttExchangeMtuRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttExchangeMtuRsp.ExpectedOpCode.ShouldHaveValue(0x03);
    }

    [Theory]
    [InlineData("034100", 65)]
    [InlineData("03410000", 65)]
    public void TryReadLittleEndian_ShouldBeValid(string hexBytes, ushort serverRxMtu)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttExchangeMtuRsp.TryReadLittleEndian(bytes, out AttExchangeMtuRsp value, out int decoded);

        success.ShouldBeTrue();
        decoded.ShouldBe(3);
        value.OpCode.ShouldBe(AttOpCode.ATT_EXCHANGE_MTU_RSP);
        value.ServerRxMtu.ShouldBe(serverRxMtu);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0341", 0)]
    // [InlineData("024100", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttExchangeMtuRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
