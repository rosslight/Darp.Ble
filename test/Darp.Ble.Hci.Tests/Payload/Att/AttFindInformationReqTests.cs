using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttFindInformationReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttFindInformationReq.ExpectedOpCode.ShouldHaveValue(0x04);
    }

    [Theory]
    [InlineData(1, 0xFFFF, "040100FFFF")]
    [InlineData(31, 0xFFFF, "041F00FFFF")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort startingHandle, ushort endingHandle, string expectedHexBytes)
    {
        var buffer = new byte[5];
        var value = new AttFindInformationReq { StartingHandle = startingHandle, EndingHandle = endingHandle };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.ShouldBe(AttOpCode.ATT_FIND_INFORMATION_REQ);
        value.GetByteCount().ShouldBe(5);
        success.ShouldBeTrue();
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[4];
        var value = new AttFindInformationReq { StartingHandle = 1, EndingHandle = 0xFFFF };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
