using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciReadLocalSupportedCommandsCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciReadLocalSupportedCommandsCommand.OpCode.ShouldHaveValue(0x0002 | (0x04 << 10));
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciReadLocalSupportedCommandsCommand();

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(0);
    }
}
