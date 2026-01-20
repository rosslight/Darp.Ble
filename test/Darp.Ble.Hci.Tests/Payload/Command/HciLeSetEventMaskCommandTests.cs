using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetEventMaskCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetEventMaskCommand.OpCode.ShouldHaveValue(0x0001 | (0x08 << 10));
    }

    [Theory]
    [InlineData((LeEventMask)0xFFFFF00000000000, "0000000000F0FFFF")]
    public void TryWriteLittleEndian_ShouldBeValid(LeEventMask mask, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(8);
        var value = new HciLeSetEventMaskCommand { Mask = mask };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(8);
        value.Mask.ShouldBe(mask);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(7);
        HciLeSetEventMaskCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
