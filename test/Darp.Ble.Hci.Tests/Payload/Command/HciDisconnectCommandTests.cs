using System.Security.Cryptography;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciDisconnectCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciDisconnectCommand.OpCode.ShouldHaveValue(0x0006 | (0x01 << 10));
    }

    [Theory]
    [InlineData(0, HciCommandStatus.RemoteUserTerminatedConnection, "000013")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort handle, HciCommandStatus reason, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(3);
        var value = new HciDisconnectCommand { ConnectionHandle = handle, Reason = reason };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(3);
        value.ConnectionHandle.ShouldBe(handle);
        value.Reason.ShouldBe(reason);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(2);
        HciDisconnectCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
