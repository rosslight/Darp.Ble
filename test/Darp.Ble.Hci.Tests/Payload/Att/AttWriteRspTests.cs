using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttWriteRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttWriteRsp.ExpectedOpCode.Should().HaveValue(0x13);
    }

    [Theory]
    [InlineData("13")]
    [InlineData("1300")]
    public void TryReadLittleEndian_ShouldBeValid(string hexBytes)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);

        bool success = AttWriteRsp.TryReadLittleEndian(
            bytes,
            out AttWriteRsp value,
            out int decoded
        );

        success.Should().BeTrue();
        decoded.Should().Be(1);
        value.OpCode.Should().Be(AttOpCode.ATT_WRITE_RSP);
    }

    [Theory]
    [InlineData("", 0)]
    // [InlineData("12", 0)] TODO: Handle parsing of invalid opCodes
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttWriteRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}
