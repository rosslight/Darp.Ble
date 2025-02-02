using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttErrorRspTests
{
    private const AttOpCode ExpectedOpCode = AttOpCode.ATT_ERROR_RSP;

    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttErrorRsp.ExpectedOpCode.Should().Be(ExpectedOpCode);
    }

    [Theory]
    [InlineData("01081C000A", AttOpCode.ATT_READ_BY_TYPE_REQ, 0x001C, AttErrorCode.AttributeNotFoundError)]
    [InlineData("01042B000A", AttOpCode.ATT_FIND_INFORMATION_REQ, 0x002B, AttErrorCode.AttributeNotFoundError)]
    [InlineData("010828000A00", AttOpCode.ATT_READ_BY_TYPE_REQ, 0x0028, AttErrorCode.AttributeNotFoundError)]
    public void TryReadLittleEndian_ShouldBeValid(
        string hexBytes,
        AttOpCode expectedRequestOpCode,
        ushort expectedHandle,
        AttErrorCode expectedErrorCode
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttErrorRsp.TryReadLittleEndian(bytes, out AttErrorRsp value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(5);
        value.OpCode.Should().Be(ExpectedOpCode);
        value.RequestOpCode.Should().Be(expectedRequestOpCode);
        value.Handle.Should().Be(expectedHandle);
        value.ErrorCode.Should().Be(expectedErrorCode);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("01042B00", 0)]
    // [InlineData("02042B000A", 0)] TODO: Handle parsing of invalid opCodes
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttErrorRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}
