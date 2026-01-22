using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciSetEventMaskCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciSetEventMaskCommand.OpCode.ShouldHaveValue(0x0001 | (0x03 << 10));
    }

    [Theory]
    [InlineData((EventMask)0xFFFFFFFFFFFFFF3F, "3FFFFFFFFFFFFFFF")]
    public void TryWriteLittleEndian_ShouldBeValid(EventMask eventMask, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(8);
        var value = new HciSetEventMaskCommand { Mask = eventMask };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(8);
        value.Mask.ShouldBe(eventMask);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(7);
        HciSetEventMaskCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
