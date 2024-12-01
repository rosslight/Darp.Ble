using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetEventMaskCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetEventMaskCommand.OpCode.Should().HaveValue(0x0001 | (0x08 << 10));
    }

    [Theory]
    [InlineData((LeEventMask)0xFFFFF00000000000, "0000000000F0FFFF")]
    public void TryEncode_ShouldBeValid(LeEventMask mask, string expectedHexBytes)
    {
        var buffer = new byte[8];
        var value = new HciLeSetEventMaskCommand
        {
            Mask = mask,
        };

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(8);
        value.Mask.Should().Be(mask);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryEncode_ShouldBeInvalid()
    {
        var buffer = new byte[7];
        HciLeSetEventMaskCommand value = default;

        bool success = value.TryEncode(buffer);
        success.Should().BeFalse();
    }
}