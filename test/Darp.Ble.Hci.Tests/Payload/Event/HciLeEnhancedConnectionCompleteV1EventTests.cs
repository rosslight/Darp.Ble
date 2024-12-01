using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciLeEnhancedConnectionCompleteV1EventTests
{
    [Fact]
    public void SubEventType_ShouldBeValid()
    {
        HciLeEnhancedConnectionCompleteV1Event.SubEventType.Should().HaveValue(0x0A);
    }

    [Theory]
    [InlineData("0A0001000001AABBCCDDEEFF0000000000000000000000000600F3010A0004",
        HciCommandStatus.Success, 0x0001, 0,
        1, 0xFFEEDDCCBBAA, 0x000000000000, 0x000000000000,
        0x0006, 0x01F3, 0x000A, 0x04)]
    public void TryDecode_HciSetEventMaskResult_ShouldBeValid(string hexBytes,
        HciCommandStatus expectedStatus,
        ushort expectedConnectionHandle,
        byte expectedRole,
        byte expectedPeerAddressType,
        ulong expectedPeerAddress,
        ulong expectedLocalResolvablePrivateAddress,
        ulong expectedPeerResolvablePrivateAddress,
        ushort expectedConnectionInterval,
        ushort expectedPeripheralLatency,
        ushort expectedSupervisionTimeout,
        byte expectedCentralClockAccuracy)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        var expectedValue = new HciLeEnhancedConnectionCompleteV1Event
        {
            SubEventCode = HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1,
            Status = expectedStatus,
            ConnectionHandle = expectedConnectionHandle,
            Role = expectedRole,
            PeerAddressType = expectedPeerAddressType,
            PeerAddress = expectedPeerAddress,
            LocalResolvablePrivateAddress = expectedLocalResolvablePrivateAddress,
            PeerResolvablePrivateAddress = expectedPeerResolvablePrivateAddress,
            ConnectionInterval = expectedConnectionInterval,
            PeripheralLatency = expectedPeripheralLatency,
            SupervisionTimeout = expectedSupervisionTimeout,
            CentralClockAccuracy = expectedCentralClockAccuracy,
        };

        bool success = Extensions.TryDecode(bytes, out HciLeEnhancedConnectionCompleteV1Event value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(31);
        value.ConnectionHandle.Should().Be(expectedValue.ConnectionHandle);
        value.SubEventCode.Should().Be(expectedValue.SubEventCode);
        value.Status.Should().Be(expectedValue.Status);
        value.ConnectionHandle.Should().Be(expectedValue.ConnectionHandle);
        value.Role.Should().Be(expectedValue.Role);
        value.PeerAddressType.Should().Be(expectedValue.PeerAddressType);
        value.PeerAddress.Should().Be(expectedValue.PeerAddress);
        value.LocalResolvablePrivateAddress.Should().Be(expectedValue.LocalResolvablePrivateAddress);
        value.PeerResolvablePrivateAddress.Should().Be(expectedValue.PeerResolvablePrivateAddress);
        value.ConnectionInterval.Should().Be(expectedValue.ConnectionInterval);
        value.PeripheralLatency.Should().Be(expectedValue.PeripheralLatency);
        value.SupervisionTimeout.Should().Be(expectedValue.SupervisionTimeout);
        value.CentralClockAccuracy.Should().Be(expectedValue.CentralClockAccuracy);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0A0001000001AABBCCDDEEFF0000000000000000000000000600F3010A00", 0)]
    public void TryDecode_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = Extensions.TryDecode(bytes, out HciLeEnhancedConnectionCompleteV1Event _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}