using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeExtendedCreateConnectionV1CommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeExtendedCreateConnectionV1Command.OpCode.ShouldHaveValue(0x0043 | (0x08 << 10));
    }

    [Theory]
    [InlineData(
        0,
        1,
        1,
        0xB335EFF8406A,
        1,
        160,
        160,
        24,
        24,
        0,
        72,
        0,
        0,
        "0001016A40F8EF35B301A000A000180018000000480000000000"
    )]
    public void TryWriteLittleEndian_ShouldBeValid(
        byte initiatorFilterPolicy,
        byte ownAddressType,
        byte peerAddressType,
        ulong peerAddress,
        byte initiatingPhys,
        ushort scanInterval,
        ushort scanWindow,
        ushort connectionIntervalMin,
        ushort connectionIntervalMax,
        ushort maxLatency,
        ushort supervisionTimeout,
        ushort minCeLength,
        ushort maxCeLength,
        string expectedHexBytes
    )
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(26);
        var value = new HciLeExtendedCreateConnectionV1Command
        {
            InitiatorFilterPolicy = initiatorFilterPolicy,
            OwnAddressType = ownAddressType,
            PeerAddressType = peerAddressType,
            PeerAddress = peerAddress,
            InitiatingPhys = initiatingPhys,
            ScanInterval = scanInterval,
            ScanWindow = scanWindow,
            ConnectionIntervalMin = connectionIntervalMin,
            ConnectionIntervalMax = connectionIntervalMax,
            MaxLatency = maxLatency,
            SupervisionTimeout = supervisionTimeout,
            MinCeLength = minCeLength,
            MaxCeLength = maxLatency,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(26);
        value.InitiatorFilterPolicy.ShouldBe(initiatorFilterPolicy);
        value.OwnAddressType.ShouldBe(ownAddressType);
        value.PeerAddressType.ShouldBe(peerAddressType);
        value.PeerAddress.ShouldBe((UInt48)peerAddress);
        value.InitiatingPhys.ShouldBe(initiatingPhys);
        value.ScanInterval.ShouldBe(scanInterval);
        value.ScanWindow.ShouldBe(scanWindow);
        value.ConnectionIntervalMin.ShouldBe(connectionIntervalMin);
        value.ConnectionIntervalMax.ShouldBe(connectionIntervalMax);
        value.MaxLatency.ShouldBe(maxLatency);
        value.SupervisionTimeout.ShouldBe(supervisionTimeout);
        value.MinCeLength.ShouldBe(minCeLength);
        value.MaxCeLength.ShouldBe(maxCeLength);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(25);
        HciLeExtendedCreateConnectionV1Command value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
