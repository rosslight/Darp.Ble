using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetExtendedScanEnableCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetExtendedScanEnableCommand.OpCode.Should().HaveValue(0x0042 | (0x08 << 10));
    }

    [Theory]
    [InlineData(1, 0, 0, 0, "010000000000")]
    public void TryWriteLittleEndian_ShouldBeValid(byte enable,
        byte filterDuplicates,
        ushort duration,
        ushort period,
        string expectedHexBytes)
    {
        var buffer = new byte[6];
        var value = new HciLeSetExtendedScanEnableCommand
        {
            Enable = enable,
            FilterDuplicates = filterDuplicates,
            Duration = duration,
            Period = period,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(6);
        value.Enable.Should().Be(enable);
        value.FilterDuplicates.Should().Be(filterDuplicates);
        value.Duration.Should().Be(duration);
        value.Period.Should().Be(period);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[5];
        HciLeSetExtendedScanEnableCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}