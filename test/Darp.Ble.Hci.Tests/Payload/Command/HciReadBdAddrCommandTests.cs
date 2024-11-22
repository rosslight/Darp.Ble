using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciReadBdAddrCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciReadBdAddrCommand.OpCode.Should().HaveValue(0x0009 | (0x04 << 10));
    }

    [Fact]
    public void TryEncode_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciReadBdAddrCommand();

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(0);
    }
}