using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciSetEventMaskCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciSetEventMaskCommand.OpCode.Should().HaveValue(0x0001 | (0x03 << 10));
    }

    [Theory]
    [InlineData((EventMask)0xFFFFFFFFFFFFFF3F, "3FFFFFFFFFFFFFFF")]
    public void TryWriteLittleEndian_ShouldBeValid(EventMask eventMask, string expectedHexBytes)
    {
        var buffer = new byte[8];
        var value = new HciSetEventMaskCommand { Mask = eventMask };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(8);
        value.Mask.Should().Be(eventMask);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[7];
        HciSetEventMaskCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
