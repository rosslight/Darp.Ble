using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttFindInformationReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttFindInformationReq.ExpectedOpCode.Should().HaveValue(0x04);
    }

    [Theory]
    [InlineData(1, 0xFFFF, "040100FFFF")]
    [InlineData(31, 0xFFFF, "041F00FFFF")]
    public void TryWriteLittleEndian_ShouldBeValid(
        ushort startingHandle,
        ushort endingHandle,
        string expectedHexBytes
    )
    {
        var buffer = new byte[5];
        var value = new AttFindInformationReq
        {
            StartingHandle = startingHandle,
            EndingHandle = endingHandle,
        };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_FIND_INFORMATION_REQ);
        value.GetByteCount().Should().Be(5);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[4];
        var value = new AttFindInformationReq { StartingHandle = 1, EndingHandle = 0xFFFF };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
