using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetRandomAddressCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetRandomAddressCommand.OpCode.Should().HaveValue(0x0005 | (0x08 << 10));
    }

    [Theory]
    [InlineData(0xF5F4F3F2F1F0, "F0F1F2F3F4F5")]
    public void TryWriteLittleEndian_ShouldBeValid(ulong address, string expectedHexBytes)
    {
        var buffer = new byte[6];
        var value = new HciLeSetRandomAddressCommand { RandomAddress = address };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(6);
        value.RandomAddress.Should().Be((UInt48)address);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[5];
        HciLeSetExtendedScanParametersCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
