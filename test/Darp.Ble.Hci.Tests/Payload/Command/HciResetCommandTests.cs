using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciResetCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciResetCommand.OpCode.Should().HaveValue(0x0003 | (0x03 << 10));
    }

    [Fact]
    public void TryEncode_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciResetCommand();

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(0);
    }
}