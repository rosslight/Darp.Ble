using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciCommandCompleteEventTests
{
    [Fact]
    public void EventCode_ShouldBeValid()
    {
        HciCommandCompleteEvent<HciSetEventMaskResult>.EventCode.ShouldHaveValue(0x0E);
    }

    [Theory]
    [InlineData("01010C00", 1, HciOpCode.HCI_Set_Event_Mask, HciCommandStatus.Success)]
    public void TryReadLittleEndian_HciSetEventMaskResult_ShouldBeValid(
        string hexBytes,
        byte expectedNumHciCommandPackets,
        HciOpCode expectedCommandOpCode,
        HciCommandStatus expectedStatus
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = HciCommandCompleteEvent<HciSetEventMaskResult>.TryReadLittleEndian(
            bytes,
            out var value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(4);
        value.NumHciCommandPackets.ShouldBe(expectedNumHciCommandPackets);
        value.CommandOpCode.ShouldBe(expectedCommandOpCode);
        value.ReturnParameters.Status.ShouldBe(expectedStatus);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("01010C", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = HciCommandCompleteEvent<HciSetEventMaskResult>.TryReadLittleEndian(
            bytes,
            out _,
            out int decoded
        );

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
