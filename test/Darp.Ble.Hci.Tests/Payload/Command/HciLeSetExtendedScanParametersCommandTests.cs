using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetExtendedScanParametersCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetExtendedScanParametersCommand.OpCode.ShouldHaveValue(0x0041 | (0x08 << 10));
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
        byte[] buffer = RandomNumberGenerator.GetBytes(8);
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
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(8);
        value.OwnAddressType.ShouldBe(ownAddressType);
        value.ScanningFilterPolicy.ShouldBe(scanningFilterPolicy);
        value.ScanPhys.ShouldBe(scanPhys);
        value.ScanType.ShouldBe(scanType);
        value.ScanInterval.ShouldBe(scanInterval);
        value.ScanWindow.ShouldBe(scanWindow);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(7);
        HciLeSetExtendedScanParametersCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
