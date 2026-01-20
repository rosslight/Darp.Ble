using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciLeEnhancedConnectionCompleteV1EventTests
{
    [Fact]
    public void SubEventType_ShouldBeValid()
    {
        HciLeEnhancedConnectionCompleteV1Event.SubEventType.ShouldHaveValue(0x0A);
    }

    [Theory]
    [InlineData(
        "0A0001000001AABBCCDDEEFF0000000000000000000000000600F3010A0004",
        HciCommandStatus.Success,
        0x0001,
        HciLeConnectionRole.Central,
        1,
        0xFFEEDDCCBBAA,
        0x000000000000,
        0x000000000000,
        0x0006,
        0x01F3,
        0x000A,
        0x04
    )]
    public void TryReadLittleEndian_HciSetEventMaskResult_ShouldBeValid(
        string hexBytes,
        HciCommandStatus expectedStatus,
        ushort expectedConnectionHandle,
        HciLeConnectionRole expectedRole,
        byte expectedPeerAddressType,
        ulong expectedPeerAddress,
        ulong expectedLocalResolvablePrivateAddress,
        ulong expectedPeerResolvablePrivateAddress,
        ushort expectedConnectionInterval,
        ushort expectedPeripheralLatency,
        ushort expectedSupervisionTimeout,
        byte expectedCentralClockAccuracy
    )
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

        bool success = Extensions.TryReadLittleEndian(
            bytes,
            out HciLeEnhancedConnectionCompleteV1Event value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(31);
        value.ConnectionHandle.ShouldBe(expectedValue.ConnectionHandle);
        value.SubEventCode.ShouldBe(expectedValue.SubEventCode);
        value.Status.ShouldBe(expectedValue.Status);
        value.ConnectionHandle.ShouldBe(expectedValue.ConnectionHandle);
        value.Role.ShouldBe(expectedValue.Role);
        value.PeerAddressType.ShouldBe(expectedValue.PeerAddressType);
        value.PeerAddress.ShouldBe(expectedValue.PeerAddress);
        value.LocalResolvablePrivateAddress.ShouldBe(expectedValue.LocalResolvablePrivateAddress);
        value.PeerResolvablePrivateAddress.ShouldBe(expectedValue.PeerResolvablePrivateAddress);
        value.ConnectionInterval.ShouldBe(expectedValue.ConnectionInterval);
        value.PeripheralLatency.ShouldBe(expectedValue.PeripheralLatency);
        value.SupervisionTimeout.ShouldBe(expectedValue.SupervisionTimeout);
        value.CentralClockAccuracy.ShouldBe(expectedValue.CentralClockAccuracy);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0A0001000001AABBCCDDEEFF0000000000000000000000000600F3010A00", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = Extensions.TryReadLittleEndian(
            bytes,
            out HciLeEnhancedConnectionCompleteV1Event _,
            out int decoded
        );

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
