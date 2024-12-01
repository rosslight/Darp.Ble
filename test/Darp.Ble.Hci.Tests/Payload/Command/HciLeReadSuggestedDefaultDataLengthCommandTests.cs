using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeReadSuggestedDefaultDataLengthCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeReadSuggestedDefaultDataLengthCommand.OpCode.Should().HaveValue(0x0023 | (0x08 << 10));
    }

    [Fact]
    public void TryEncode_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciLeReadSuggestedDefaultDataLengthCommand();

        bool success = value.TryEncode(buffer);
        success.Should().BeTrue();
        value.GetLength().Should().Be(0);
    }
}