using System.Security.Cryptography;
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
    public void TryWriteLittleEndian_ShouldBeValid(LeEventMask mask, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(8);
        var value = new HciLeSetEventMaskCommand { Mask = mask };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(8);
        value.Mask.Should().Be(mask);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(7);
        HciLeSetEventMaskCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
