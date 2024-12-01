using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeReadBufferSizeCommandV1Tests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeReadBufferSizeCommandV1.OpCode.Should().HaveValue(0x0002 | (0x08 << 10));
    }

    [Fact]
    public void TryEncode_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciLeReadBufferSizeCommandV1();

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(0);
    }
}