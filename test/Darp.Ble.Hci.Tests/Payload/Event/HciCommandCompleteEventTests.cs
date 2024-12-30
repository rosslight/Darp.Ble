using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciCommandCompleteEventTests
{
    [Fact]
    public void EventCode_ShouldBeValid()
    {
        HciCommandCompleteEvent<HciSetEventMaskResult>.EventCode.Should().HaveValue(0x0E);
    }

    [Theory]
    [InlineData("01010C00", 1, HciOpCode.HCI_Set_Event_Mask, HciCommandStatus.Success)]
    public void TryReadLittleEndian_HciSetEventMaskResult_ShouldBeValid(string hexBytes,
        byte expectedNumHciCommandPackets,
        HciOpCode expectedCommandOpCode,
        HciCommandStatus expectedStatus)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = HciCommandCompleteEvent<HciSetEventMaskResult>.TryReadLittleEndian(bytes, out var value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(4);
        value.NumHciCommandPackets.Should().Be(expectedNumHciCommandPackets);
        value.CommandOpCode.Should().Be(expectedCommandOpCode);
        value.ReturnParameters.Status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("01010C", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = HciCommandCompleteEvent<HciSetEventMaskResult>.TryReadLittleEndian(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}