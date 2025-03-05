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
    public void TryWriteLittleEndian_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciReadBdAddrCommand();

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(0);
    }
}
