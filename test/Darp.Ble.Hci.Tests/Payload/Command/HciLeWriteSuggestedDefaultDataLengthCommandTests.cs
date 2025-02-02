using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeWriteSuggestedDefaultDataLengthCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeWriteSuggestedDefaultDataLengthCommand
            .OpCode.Should()
            .HaveValue(0x0024 | (0x08 << 10));
    }

    [Theory]
    [InlineData(65, 328, "41004801")]
    public void TryWriteLittleEndian_ShouldBeValid(
        ushort suggestedMaxTxOctets,
        ushort suggestedMaxTxTime,
        string expectedHexBytes
    )
    {
        var buffer = new byte[4];
        var value = new HciLeWriteSuggestedDefaultDataLengthCommand
        {
            SuggestedMaxTxOctets = suggestedMaxTxOctets,
            SuggestedMaxTxTime = suggestedMaxTxTime,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(4);
        value.SuggestedMaxTxOctets.Should().Be(suggestedMaxTxOctets);
        value.SuggestedMaxTxTime.Should().Be(suggestedMaxTxTime);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[3];
        HciLeWriteSuggestedDefaultDataLengthCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
