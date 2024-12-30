using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetDataLengthCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetDataLengthCommand.OpCode.Should().HaveValue(0x0022 | (0x08 << 10));
    }

    [Theory]
    [InlineData(0, 65, 328, "000041004801")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort handle, ushort txOctets, ushort txTime, string expectedHexBytes)
    {
        var buffer = new byte[6];
        var value = new HciLeSetDataLengthCommand
        {
            ConnectionHandle = handle,
            TxOctets = txOctets,
            TxTime = txTime,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(6);
        value.ConnectionHandle.Should().Be(handle);
        value.TxOctets.Should().Be(txOctets);
        value.TxTime.Should().Be(txTime);
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[5];
        HciLeSetDataLengthCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}