using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciCommandStatusEventTests
{
    [Fact]
    public void EventCode_ShouldBeValid()
    {
        HciCommandStatusEvent.EventCode.Should().HaveValue(0x0F);
    }

    [Theory]
    [InlineData("00010604", HciCommandStatus.Success, 1, HciOpCode.HCI_Disconnect)]
    public void TryReadLittleEndian_HciSetEventMaskResult_ShouldBeValid(
        string hexBytes,
        HciCommandStatus expectedStatus,
        byte expectedNumHciCommandPackets,
        HciOpCode expectedCommandOpCode
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        var expectedValue = new HciCommandStatusEvent
        {
            Status = expectedStatus,
            NumHciCommandPackets = expectedNumHciCommandPackets,
            CommandOpCode = expectedCommandOpCode,
        };

        bool success = Extensions.TryReadLittleEndian(bytes, out HciCommandStatusEvent value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(4);
        value.Status.Should().Be(expectedValue.Status);
        value.NumHciCommandPackets.Should().Be(expectedValue.NumHciCommandPackets);
        value.CommandOpCode.Should().Be(expectedValue.CommandOpCode);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("01010C", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = Extensions.TryReadLittleEndian(bytes, out HciCommandStatusEvent _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}
