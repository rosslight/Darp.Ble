using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetExtendedScanParametersCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetExtendedScanParametersCommand.OpCode.Should().HaveValue(0x0041 | (0x08 << 10));
    }

    [Theory]
    [InlineData(1, 0, 1, 0, 160, 160, "01000100A000A000")]
    public void TryWriteLittleEndian_ShouldBeValid(
        byte ownAddressType,
        byte scanningFilterPolicy,
        byte scanPhys,
        byte scanType,
        ushort scanInterval,
        ushort scanWindow,
        string expectedHexBytes
    )
    {
        var buffer = new byte[8];
        var value = new HciLeSetExtendedScanParametersCommand
        {
            OwnAddressType = ownAddressType,
            ScanningFilterPolicy = scanningFilterPolicy,
            ScanPhys = scanPhys,
            ScanType = scanType,
            ScanInterval = scanInterval,
            ScanWindow = scanWindow,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(8);
        value.OwnAddressType.Should().Be(ownAddressType);
        value.ScanningFilterPolicy.Should().Be(scanningFilterPolicy);
        value.ScanPhys.Should().Be(scanPhys);
        value.ScanType.Should().Be(scanType);
        value.ScanInterval.Should().Be(scanInterval);
        value.ScanWindow.Should().Be(scanWindow);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[7];
        HciLeSetExtendedScanParametersCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
