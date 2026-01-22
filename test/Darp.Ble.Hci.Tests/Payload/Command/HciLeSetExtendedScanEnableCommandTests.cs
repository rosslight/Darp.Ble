using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetExtendedScanEnableCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetExtendedScanEnableCommand.OpCode.ShouldHaveValue(0x0042 | (0x08 << 10));
    }

    [Theory]
    [InlineData(1, 0, 0, 0, "010000000000")]
    public void TryWriteLittleEndian_ShouldBeValid(
        byte enable,
        byte filterDuplicates,
        ushort duration,
        ushort period,
        string expectedHexBytes
    )
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(6);
        var value = new HciLeSetExtendedScanEnableCommand
        {
            Enable = enable,
            FilterDuplicates = filterDuplicates,
            Duration = duration,
            Period = period,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(6);
        value.Enable.ShouldBe(enable);
        value.FilterDuplicates.ShouldBe(filterDuplicates);
        value.Duration.ShouldBe(duration);
        value.Period.ShouldBe(period);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(5);
        HciLeSetExtendedScanEnableCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
