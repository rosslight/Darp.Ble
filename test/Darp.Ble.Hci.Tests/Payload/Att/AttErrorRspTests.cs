using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttErrorRspTests
{
    private const AttOpCode ExpectedOpCode = AttOpCode.ATT_ERROR_RSP;

    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttErrorRsp.ExpectedOpCode.ShouldBe(ExpectedOpCode);
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

        success.ShouldBeTrue();
        decoded.ShouldBe(5);
        value.OpCode.ShouldBe(ExpectedOpCode);
        value.RequestOpCode.ShouldBe(expectedRequestOpCode);
        value.Handle.ShouldBe(expectedHandle);
        value.ErrorCode.ShouldBe(expectedErrorCode);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("01042B00", 0)]
    // [InlineData("02042B000A", 0)] TODO: Handle parsing of invalid opCodes
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttErrorRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
