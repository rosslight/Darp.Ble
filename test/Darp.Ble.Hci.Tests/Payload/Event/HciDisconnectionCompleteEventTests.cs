using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciDisconnectionCompleteEventTests
{
    [Fact]
    public void EventCode_ShouldBeValid()
    {
        HciDisconnectionCompleteEvent.EventCode.Should().HaveValue(0x05);
    }

    [Theory]
    [InlineData("010013", 0x0001, HciCommandStatus.RemoteUserTerminatedConnection)]
    public void TryDecode_HciSetEventMaskResult_ShouldBeValid(string hexBytes,
        ushort expectedConnectionHandle,
        HciCommandStatus expectedReason)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        var expectedValue = new HciDisconnectionCompleteEvent
        {
            ConnectionHandle = expectedConnectionHandle,
            Reason = expectedReason,
        };

        bool success = Extensions.TryDecode(bytes, out HciDisconnectionCompleteEvent value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(3);
        value.ConnectionHandle.Should().Be(expectedValue.ConnectionHandle);
        value.Reason.Should().Be(expectedValue.Reason);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0100", 0)]
    public void TryDecode_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = Extensions.TryDecode(bytes, out HciCommandStatusEvent _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}