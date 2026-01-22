using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttFindByTypeValueRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttFindByTypeValueRsp.ExpectedOpCode.ShouldHaveValue(0x07);
    }

    [Theory]
    [InlineData("071700FFFF", 5, 0x0017, 0xFFFF)]
    [InlineData("071700FFFF00", 5, 0x0017, 0xFFFF)]
    public void TryReadLittleEndian_ShouldBeValid(
        string hexBytes,
        int expectedBytesRead,
        ushort foundAttributeHandle,
        ushort groupEndHandle
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttFindByTypeHandlesInformation[] handlesInformation =
        [
            new() { FoundAttributeHandle = foundAttributeHandle, GroupEndHandle = groupEndHandle },
        ];

        bool success = AttFindByTypeValueRsp.TryReadLittleEndian(
            bytes,
            out AttFindByTypeValueRsp value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(expectedBytesRead);
        value.OpCode.ShouldBe(AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP);
        value.HandlesInformationList.ShouldBe(handlesInformation);
    }

    [Theory]
    [InlineData("", 0)]
    // [InlineData("061700FFFF", 0)] TODO: Handle parsing of invalid opCodes
    [InlineData("071700FF", 1)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttFindByTypeValueRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
