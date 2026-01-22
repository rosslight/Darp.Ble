using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttWriteRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttWriteRsp.ExpectedOpCode.ShouldHaveValue(0x13);
    }

    [Theory]
    [InlineData("13")]
    [InlineData("1300")]
    public void TryReadLittleEndian_ShouldBeValid(string hexBytes)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);

        bool success = AttWriteRsp.TryReadLittleEndian(bytes, out AttWriteRsp value, out int decoded);

        success.ShouldBeTrue();
        decoded.ShouldBe(1);
        value.OpCode.ShouldBe(AttOpCode.ATT_WRITE_RSP);
    }

    [Theory]
    [InlineData("", 0)]
    // [InlineData("12", 0)] TODO: Handle parsing of invalid opCodes
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttWriteRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
