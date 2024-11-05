using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciDisconnectCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciDisconnectCommand.OpCode.Should().HaveValue(0x0006 | (0x01 << 10));
    }

    [Theory]
    [InlineData(0, HciCommandStatus.RemoteUserTerminatedConnection, "000013")]
    public void TryEncode_ShouldBeValid(ushort handle, HciCommandStatus reason, string expectedHexBytes)
    {
        var buffer = new byte[3];
        var value = new HciDisconnectCommand
        {
            ConnectionHandle = handle,
            Reason = reason,
        };

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(3);
        value.ConnectionHandle.Should().Be(handle);
        value.Reason.Should().Be(reason);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryEncode_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        HciDisconnectCommand value = default;

        bool success = value.TryEncode(buffer);
        success.Should().BeFalse();
    }
}