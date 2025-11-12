using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeReadPhyCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeReadPhyCommand.OpCode.Should().HaveValue(0x0030 | (0x08 << 10));
    }

    [Theory]
    [InlineData(0x0000, "0000")]
    [InlineData(0x00EF, "EF00")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort handle, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(2);
        var value = new HciLeReadPhyCommand { ConnectionHandle = handle };

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeTrue();
        value.GetByteCount().Should().Be(2);
        value.ConnectionHandle.Should().Be(handle);

        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(1);
        HciLeReadPhyCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
