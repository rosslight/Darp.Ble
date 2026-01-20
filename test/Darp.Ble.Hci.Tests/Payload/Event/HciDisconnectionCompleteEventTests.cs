using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciDisconnectionCompleteEventTests
{
    [Fact]
    public void EventCode_ShouldBeValid()
    {
        HciDisconnectionCompleteEvent.EventCode.ShouldHaveValue(0x05);
    }

    [Theory]
    [InlineData("00010013", 0x0001, HciCommandStatus.RemoteUserTerminatedConnection)]
    public void TryReadLittleEndian_HciSetEventMaskResult_ShouldBeValid(
        string hexBytes,
        ushort expectedConnectionHandle,
        HciCommandStatus expectedReason
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        var expectedValue = new HciDisconnectionCompleteEvent
        {
            Status = HciCommandStatus.Success,
            ConnectionHandle = expectedConnectionHandle,
            Reason = expectedReason,
        };

        bool success = Extensions.TryReadLittleEndian(bytes, out HciDisconnectionCompleteEvent value, out int decoded);

        success.ShouldBeTrue();
        decoded.ShouldBe(4);
        value.ConnectionHandle.ShouldBe(expectedValue.ConnectionHandle);
        value.Reason.ShouldBe(expectedValue.Reason);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0100", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = Extensions.TryReadLittleEndian(bytes, out HciCommandStatusEvent _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
